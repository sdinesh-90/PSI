program Hello;
const
 pi = 3.14;
 i = 10;
var
 j : real;
function func1 (scale: real ) : real;
    var
    pi : real;
    begin
    pi := i;
    writeln (pi);
    func1 := pi
    end;
begin
  writeln ("Hello World");
  j := 3.1;
  writeln (j);
  writeln (func1 (i));
  func1 (j)
end.
