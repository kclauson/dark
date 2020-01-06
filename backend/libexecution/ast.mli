open Core_kernel

val set_expr :
     search:Types.id
  -> replacement:Types.RuntimeT.expr
  -> Types.RuntimeT.expr
  -> Types.RuntimeT.expr

val blank_to_option : 'a Types.or_blank -> 'a option

val blank_to_id : 'a Types.or_blank -> Types.id

val blank_map : f:('a -> 'b) -> 'a Types.or_blank -> 'b Types.or_blank

val blank_to_string : string Types.or_blank -> string

val traverse :
     f:(Types.RuntimeT.expr -> Types.RuntimeT.expr)
  -> Types.RuntimeT.expr
  -> Types.RuntimeT.expr

val iter : f:(Types.RuntimeT.expr -> unit) -> Types.RuntimeT.expr -> unit

val execute_dblock :
     state:Types.RuntimeT.exec_state
  -> Types.RuntimeT.dblock_args
  -> Types.RuntimeT.dval list
  -> Types.RuntimeT.dval

val execute_ast :
     state:Types.RuntimeT.exec_state
  -> input_vars:Types.RuntimeT.input_vars
  -> Types.RuntimeT.expr
  -> Types.RuntimeT.dval

val execute_fn :
     state:Types.RuntimeT.exec_state
  -> string
  -> Types.id
  -> Types.RuntimeT.dval list
  -> Types.RuntimeT.dval
