// ⓅⓈⒾ  ●  Pascal Language System  ●  Academy'23
// PSIPrint.cs ~ Prints a PSI syntax tree in Pascal format
// ─────────────────────────────────────────────────────────────────────────────
namespace PSI;

public class PSIPrint : Visitor<StringBuilder> {
   public override StringBuilder Visit (NProgram p) {
      Write ($"program {p.Name}; ");
      Visit (p.Block);
      return Write (".");
   }

   public override StringBuilder Visit (NBlock b) 
      => Visit (b.Decls, b.Body);

   public override StringBuilder Visit (NDeclarations d) {
      if (d.Vars.Length > 0) {
         NWrite ("var"); N++;
         foreach (var g in d.Vars.GroupBy (a => a.Type))
            NWrite ($"{g.Select (a => a.Name).ToCSV ()} : {g.Key};");
         N--;
      }
      return Visit (d.FnProcVars);
   }

   public override StringBuilder Visit (NVarDecl d)
      => NWrite ($"{d.Name} : {d.Type}");

   public override StringBuilder Visit (NCompoundStmt b) {
      NWrite ("begin"); N++;  Visit (b.Stmts); N--; return NWrite ("end"); 
   }

   public override StringBuilder Visit (NAssignStmt a) {
      NWrite ($"{a.Name} := "); a.Expr.Accept (this); return Write (";");
   }

   public override StringBuilder Visit (NWriteStmt w) {
      NWrite (w.NewLine ? "WriteLn (" : "Write (");
      for (int i = 0; i < w.Exprs.Length; i++) {
         if (i > 0) Write (", ");
         w.Exprs[i].Accept (this);
      }
      return Write (");");
   }

   public override StringBuilder Visit (NLiteral t)
      => Write (t.Value.ToString ());

   public override StringBuilder Visit (NIdentifier d)
      => Write (d.Name.Text);

   public override StringBuilder Visit (NUnary u) {
      Write (u.Op.Text); return u.Expr.Accept (this);
   }

   public override StringBuilder Visit (NBinary b) {
      Write ("("); b.Left.Accept (this); Write ($" {b.Op.Text} ");
      b.Right.Accept (this); return Write (")");
   }

   public override StringBuilder Visit (NFnCall f) {
      Write ($"{f.Name} (");
      for (int i = 0; i < f.Params.Length; i++) {
         if (i > 0) Write (", "); f.Params[i].Accept (this);
      }
      return Write (")");
   }

   // procedure Greeter (msg: string);
   // var
   //    name: string;
   // begin
   //    write ("Enter your name: ");
   //    read (name);
   //    write ("Hello, ", name, ". ", msg);
   // end
   public override StringBuilder Visit (NFnDecl d) {
      bool proc = d.Type is NType.Void;
      NWrite ($"{(proc ? "procedure" : "function")} {d.Name} (");
      bool first = true;
      foreach (var group in d.Params.GroupBy (a => a.Type)) {
         if (!first) Write ("; "); first = false;
         Write ($"{group.Select (a => a.Name).ToCSV ()} : {group.Key}");
      }
      Write ($"){(proc ? "" : $" : {d.Type}")};");
      return Visit (d.Block);
   }

   // read (name, age);
   public override StringBuilder Visit (NReadStmt r)
      => NWrite ($"read ({r.Vars.ToCSV ()});");

   // Fibo ();
   // Fibo (1, 2);
   public override StringBuilder Visit (NCallStmt c) {
      NWrite ($"{c.FuncName} (");
      for (int i = 0; i < c.Arguments.Length; i++) {
         if (i > 0) Write (", ");
         Visit (c.Arguments[i]);
      }
      return Write (");");
   }

   // for i := 1 to 20 do 
   //    a := a + b;
   public override StringBuilder Visit (NForStmt f) {
      NWrite ($"for {f.Name} := ");
      Visit (f.FromExpr);
      Write ($" {(f.Down ? "downto" : "to")} ");
      Visit (f.ToExpr); Write (" do"); N++;
      Visit (f.Stmt); N--;
      return S;
   }

   // if i < 12 then
   //    i := 12;
   // else 
   //    j := 13;
   public override StringBuilder Visit (NIFElseStmt i) {
      NWrite ("if "); Visit (i.Condition); Write (" then"); N++;
      Visit (i.ThenStmt); N--;
      if (i.ElseStmt != null) {
         NWrite ("else"); N++;
         Visit (i.ElseStmt); N--;
      }
      return S;
   }

   // repeat 
   //    writeln ("Hello");
   //    j := j - 1;
   // until j <= 0;
   public override StringBuilder Visit (NRepeatStmt r) {
      NWrite ("repeat"); N++;
      Visit (r.Stmts); N--;
      NWrite ($"until ");
      return Visit (r.Condition);
   }

   // while j < 20 do begin
   //    k := k + j;
   //    j := j - 1;
   // end;
   public override StringBuilder Visit (NWhileStmt w) {
      NWrite ("while "); Visit (w.Condition); Write (" do"); N++;
      Visit (w.Stmt); N--;
      return S;
   }

   StringBuilder Visit (params Node[] nodes) {
      nodes.ForEach (a => a.Accept (this));
      return S;
   }

   // Writes in a new line
   StringBuilder NWrite (string txt) 
      => Write ($"\n{new string (' ', N * 3)}{txt}");
   int N;   // Indent level

   // Continue writing on the same line
   StringBuilder Write (string txt) {
      Console.Write (txt);
      S.Append (txt);
      return S;
   }

   readonly StringBuilder S = new ();
}