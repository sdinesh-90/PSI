﻿// ⓅⓈⒾ  ●  Pascal Language System  ●  Academy'23
// TypeAnalyze.cs ~ Type checking, type coercion
// ─────────────────────────────────────────────────────────────────────────────
namespace PSI;
using static NType;
using static Token.E;

public class TypeAnalyze : Visitor<NType> {
   public TypeAnalyze () {
      mSymbols = SymTable.Root;
   }
   SymTable mSymbols;

   #region Declarations ------------------------------------
   public override NType Visit (NProgram p) => VisitBlock (p, p.Block);
   
   public override NType Visit (NBlock b) {
      mSymbols = new SymTable { Parent = mSymbols, Source = mSource };
      Visit (b.Declarations); Visit (b.Body);
      mSymbols = mSymbols.Parent;
      return Void;
   }

   public override NType Visit (NDeclarations d) {
      Visit (d.Consts); Visit (d.Vars); return Visit (d.Funcs);
   }

   public override NType Visit (NConstDecl c) {
      Check (c.Name);
      mSymbols.Consts.Add (c);
      return Visit (c.Value);
   }

   public override NType Visit (NVarDecl d) {
      Check (d.Name);
      mSymbols.Vars.Add (d);
      return d.Type;
   }

   public override NType Visit (NFnDecl f) {
      Check (f.Name);
      mSymbols.Funcs.Add (f);
      if (f.Body != null) VisitBlock (f, f.Body);
      return f.Return;
   }
   #endregion

   #region Implementation ----------------------------------
   void Check (Token name) {
      var s = mSymbols.Find (name.Text, false);
      if (s != null) throw new ParseException (name, $"Duplicate identifier \"{name.Text}\"");
   }

   NType FindVariable (Token name) => mSymbols.Find (name.Text) switch {
      NVarDecl v when v.Assigned => v.Type,
      NConstDecl c => c.Type,
      NVarDecl => throw new ParseException (name, UnAssignedError),
      _ => throw new ParseException (name, UnKnownVariable)
   };

   void VisitParameters (Token fnName, NExpr[] parameters, NType[] paramTypes) {
      if (parameters.Length != paramTypes.Length)
         throw new ParseException (fnName, "Parameter mismatch");
      for (int i = 0; i < parameters.Length; i++) {
         NExpr param = parameters[i];
         param.Accept (this);
         parameters[i] = AddTypeCast (fnName, param, paramTypes[i]);
      }
   }

   NType VisitBlock (Node source, NBlock block) {
      mSource = source;
      return Visit (block);
   }
   #endregion

   #region Statements --------------------------------------
   public override NType Visit (NCompoundStmt b)
      => Visit (b.Stmts);

   public override NType Visit (NAssignStmt a) {
      NType type;
      if (mSymbols.Find (a.Name.Text) is NVarDecl v) { type = v.Type; v.Assigned = true; }
      else if (mSymbols.Source is NFnDecl fn && fn.Name.Text.EqualsIC (a.Name.Text)) {
         type = fn.Return; fn.Assigned = true;
      } else throw new ParseException (a.Name, UnKnownVariable);
      a.Expr.Accept (this);
      a.Expr = AddTypeCast (a.Name, a.Expr, type);
      return type;
   }
   
   NExpr AddTypeCast (Token token, NExpr source, NType target) {
      if (source.Type == target) return source;
      bool valid = (source.Type, target) switch {
         (Int, Real) or (Char, Int) or (Char, String) => true,
         _ => false
      };
      if (!valid) throw new ParseException (token, "Invalid type");
      return new NTypeCast (source) { Type = target };
   }

   public override NType Visit (NWriteStmt w)
      => Visit (w.Exprs);

   public override NType Visit (NIfStmt f) {
      f.Condition.Accept (this);
      f.IfPart.Accept (this); f.ElsePart?.Accept (this);
      return Void;
   }

   public override NType Visit (NForStmt f) {
      f.Start.Accept (this); f.End.Accept (this); f.Body.Accept (this);
      return Void;
   }

   public override NType Visit (NReadStmt r) => Void;

   public override NType Visit (NWhileStmt w) {
      w.Condition.Accept (this); w.Body.Accept (this);
      return Void; 
   }

   public override NType Visit (NRepeatStmt r) {
      Visit (r.Stmts); r.Condition.Accept (this);
      return Void;
   }

   public override NType Visit (NCallStmt c) {
      if (mSymbols.Find (c.Name.Text) is not NFnDecl fn)
         throw new ParseException (c.Name, UnKnownFunction);
      if (fn.Return != Void && !fn.Assigned) throw new ParseException (c.Name, UnAssignedError);
      VisitParameters (c.Name, c.Params, fn.Params.Select (a => a.Type).ToArray ());
      return Void;
   }
   #endregion

   #region Expression --------------------------------------
   public override NType Visit (NLiteral t) {
      t.Type = t.Value.Kind switch {
         L_INTEGER => Int, L_REAL => Real, L_BOOLEAN => Bool, L_STRING => String,
         L_CHAR => Char, _ => Error,
      };
      return t.Type;
   }

   public override NType Visit (NUnary u) 
      => u.Expr.Accept (this);

   public override NType Visit (NBinary bin) {
      NType a = bin.Left.Accept (this), b = bin.Right.Accept (this);
      bin.Type = (bin.Op.Kind, a, b) switch {
         (ADD or SUB or MUL or DIV, Int or Real, Int or Real) when a == b => a,
         (ADD or SUB or MUL or DIV, Int or Real, Int or Real) => Real,
         (MOD, Int, Int) => Int,
         (ADD, String, _) => String, 
         (ADD, _, String) => String,
         (LT or LEQ or GT or GEQ, Int or Real, Int or Real) => Bool,
         (LT or LEQ or GT or GEQ, Int or Real or String or Char, Int or Real or String or Char) when a == b => Bool,
         (EQ or NEQ, _, _) when a == b => Bool,
         (EQ or NEQ, Int or Real, Int or Real) => Bool,
         (AND or OR, Int or Bool, Int or Bool) when a == b => a,
         _ => Error,
      };
      if (bin.Type == Error)
         throw new ParseException (bin.Op, "Invalid operands");
      var (acast, bcast) = (bin.Op.Kind, a, b) switch {
         (_, Int, Real) => (Real, Void),
         (_, Real, Int) => (Void, Real), 
         (_, String, not String) => (Void, String),
         (_, not String, String) => (String, Void),
         _ => (Void, Void)
      };
      if (acast != Void) bin.Left = new NTypeCast (bin.Left) { Type = acast };
      if (bcast != Void) bin.Right = new NTypeCast (bin.Right) { Type = bcast };
      return bin.Type;
   }

   public override NType Visit (NIdentifier d)
      => d.Type = FindVariable (d.Name);

   public override NType Visit (NFnCall f) {
      if (mSymbols.Find (f.Name.Text) is not NFnDecl fn)
         throw new ParseException (f.Name, UnKnownFunction);
      if (!fn.Assigned) throw new ParseException (f.Name, UnAssignedError);
      VisitParameters (f.Name, f.Params, fn.Params.Select (a => a.Type).ToArray ());
      return f.Type = fn.Return;
   }

   public override NType Visit (NTypeCast c) {
      c.Expr.Accept (this); return c.Type;
   }
   #endregion

   NType Visit (IEnumerable<Node> nodes) {
      foreach (var node in nodes) node.Accept (this);
      return Void;
   }
   Node? mSource;
   const string UnAssignedError = "Use of unassigned variable";
   const string UnKnownVariable = "Unknown variable", UnKnownFunction = "Unknown function";
}
