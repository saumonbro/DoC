proc returnInt(i : i32) : i32
{
  let b : { i : i32,b : string } :=  { i : 5, c : " " } ;
  let c : { i : i32, b : string } := b;
  if ((c.i))
  {
    return 5;
  }
  else
  {
    {
      return 9 + c.i;
    };
  };
  let a : i32 := 5 - 2 + returnInt(67);
  return i + a + 5 * 2;
}

