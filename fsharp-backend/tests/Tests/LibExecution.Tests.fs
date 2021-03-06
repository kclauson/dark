module Tests.LibExecution

// Create test cases from .tests files in the tests/stdlib dir

open Expecto
open Prelude

module R = LibExecution.RuntimeTypes
module P = LibBackend.ProgramSerialization.ProgramTypes

// Remove random things like IDs to make the tests stable
let normalizeDvalResult (dv : R.Dval) : R.Dval =
  match dv with
  | R.DFakeVal (R.DError (R.JustAString (source, str))) ->
      R.DFakeVal(R.DError(R.JustAString(R.SourceNone, str)))
  | R.DFakeVal (R.DError (errorVal)) ->
      R.DFakeVal(R.DError(R.JustAString(R.SourceNone, errorVal.ToString())))
  | dv -> dv

open LibExecution.RuntimeTypes

let rec dvalEquals (left : Dval) (right : Dval) (msg : string) : unit =
  let de l r = dvalEquals l r msg

  match left, right with
  | DFloat l, DFloat r -> Expect.floatClose Accuracy.veryHigh l r msg
  | DResult (Ok l), DResult (Ok r) -> de l r
  | DResult (Error l), DResult (Error r) -> de l r
  | DOption (Some l), DOption (Some r) -> de l r
  | DList ls, DList rs -> List.iter2 de ls rs
  | DObj ls, DObj rs ->
      List.iter2
        (fun (k1, v1) (k2, v2) ->
          Expect.equal k1 k2 msg
          de v1 v2)
        (Map.toList ls)
        (Map.toList rs)
  | DHttpResponse (sc1, h1, b1), DHttpResponse (sc2, h2, b2) ->
      Expect.equal sc1 sc2 msg
      Expect.equal h1 h2 msg
      de b1 b2
  // Keep for exhaustiveness checking
  | DHttpResponse _, _
  | DObj _, _
  | DList _, _
  | DResult _, _
  | DOption _, _
  // All others can be directly compared
  | DInt _, _
  | DBool _, _
  | DFloat _, _
  | DNull, _
  | DStr _, _
  | DChar _, _
  | DFnVal _, _
  | DFakeVal _, _
  | DDB _, _
  | DUuid _, _
  | DBytes _, _ -> Expect.equal left right msg


let t (comment : string) (code : string) : Test =
  let name = $"{comment} ({code})"

  if code.StartsWith "//" then
    ptestTask name { return (Expect.equal "skipped" "skipped" "") }
  else
    testTask name {
      try
        let fns =
          LibExecution.StdLib.StdLib.fns
          @ LibBackend.StdLib.StdLib.fns @ Tests.LibTest.fns

        let source = FSharpToExpr.parse code
        let actualProg, expectedResult = FSharpToExpr.convertToTest source
        let tlid = id 7
        let! actual = LibExecution.Execution.run tlid [] fns actualProg
        let! expected = LibExecution.Execution.run tlid [] fns expectedResult
        let actual = normalizeDvalResult actual
        //let str = $"{source} => {actualProg} = {expectedResult}"
        let str = $"{actualProg}\n = \n{expectedResult}"
        return (dvalEquals actual expected str)

      with e -> return (Expect.equal "" e.Message "Error message")
    }


// Read all test files. Test file format is as follows:
//
// Lines with just comments or whitespace are ignored
// Tests are made up of code and comments, comments are used as names
//
// Test indicators:
//   [tests.name] denotes that the following lines (until the next test
//   indicator) are single line tests, that are all part of the test group
//   named "name". Single line tests should evaluate to true, and may have a
//   comment at the end, which will be the test name
//
//   [test.name] indicates that the following lines, up until the next test
//   indicator, are all a single test named "name", and should be parsed as
//   one.
let fileTests () : Test =
  let dir = "tests/testfiles/"

  System.IO.Directory.GetFiles(dir, "*")
  |> Array.map
       (fun file ->
         let filename = System.IO.Path.GetFileName file
         let currentTestName = ref ""
         let currentTests = ref []
         let singleTestMode = ref false
         let currentTestString = ref "" // keep track of the current [test]
         let allTests = ref []

         let finish () =
           // Add the current work to allTests, and clear
           let newTestCase =
             if !singleTestMode then
               // Add a single test case
               t !currentTestName !currentTestString
             else
               // Put currentTests in a group and add them
               testList !currentTestName !currentTests

           allTests := !allTests @ [ newTestCase ]

           // Clear settings
           currentTestName := ""
           singleTestMode := false
           currentTestString := ""
           currentTests := []

         (dir + filename)
         |> System.IO.File.ReadLines
         |> Seq.iteri
              (fun i line ->
                let i = i + 1

                match line with
                // [tests] indicator
                | Regex "^\[tests\.(.*)\]$" [ name ] ->
                    finish ()
                    currentTestName := name
                // [test] indicator
                | Regex "^\[test\.(.*)\]$" [ name ] ->
                    finish ()
                    singleTestMode := true
                    currentTestName := name
                // Skip comment-only lines
                | Regex "^\s*//.*" [] -> ()
                // Append to the current test string
                | _ when !singleTestMode ->
                    currentTestString := !currentTestString + line
                // Skip whitespace lines
                | Regex "^\s*$" [] -> ()
                // 1-line test
                | Regex "^(.*)\s*$" [ code ] ->
                    currentTests := !currentTests @ [ t $"line {i}" code ]
                // 1-line test w/ comment
                | Regex "^(.*)\s*//\s*(.*)$" [ code; comment ] ->
                    currentTests
                    := !currentTests @ [ t $"{comment} (line {i})" code ]
                | _ -> raise (System.Exception $"can't parse line {i}: {line}"))

         finish ()
         testList $"Tests from {filename}" !allTests)
  |> Array.toList
  |> testList "All files"

let testMany (name : string) (fn : 'a -> 'b) (values : List<'a * 'b>) =
  testList
    name
    (List.mapi
      (fun i (input, expected) ->
        test $"{name}[{i}]: ({input}) -> {expected}" {
          Expect.equal (fn input) expected "" })
      values)

let fqFnName =
  testMany
    "FQFnName.ToString"
    (fun (name : FQFnName.T) -> name.ToString())
    [ (FQFnName.stdlibName "" "++" 0), "++_v0"
      (FQFnName.stdlibName "" "!=" 0), "!=_v0"
      (FQFnName.stdlibName "" "&&" 0), "&&_v0"
      (FQFnName.stdlibName "" "toString" 0), "toString_v0"
      (FQFnName.stdlibName "String" "append" 1), "String::append_v1" ]

let backendFqFnName =
  testMany
    "ProgramTypes.FQFnName.ToString"
    (fun (name : P.FQFnName.T) -> name.ToString())
    [ (P.FQFnName.stdlibName "" "++" 0), "++_v0"
      (P.FQFnName.stdlibName "" "!=" 0), "!=_v0"
      (P.FQFnName.stdlibName "" "&&" 0), "&&_v0"
      (P.FQFnName.stdlibName "" "toString" 0), "toString_v0"
      (P.FQFnName.stdlibName "String" "append" 1), "String::append_v1" ]


let tests = testList "LibExecution" [ fqFnName; backendFqFnName; fileTests () ]
