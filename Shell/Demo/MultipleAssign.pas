program Hello;
const
 pi = 3.14;
 i = 10;
var
 j : real;
function func1 (scale: real ) : real;
    var
    pi : real;
    str : string;
    function func2 (errorCode: integer) : string;
      var
      format: integer;
      begin
      writeln ("Error");
      pi := 10;
      if pi = 10 then
       func2 := "Chec";
      end;
      function func3 (errorCode: integer) : string;
      var
      format: integer;
      begin
      writeln ("Error");
      func3 := "Chec";
      end;
    begin
    pi := i;
    writeln (pi);
    func1 := pi;
    func2 (i);
    str := func3 (i);
    end;
begin
  writeln ("Hello World");
  j := 3.1;
  writeln (j);
  writeln (func1 (i));
  func1 (j)
end.
