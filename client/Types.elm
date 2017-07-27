module Types exposing (..)

import Dict exposing (Dict)
import Http
import Dom
import Keyboard
import Mouse



type alias Name = String
type alias FieldName = Name
type alias ParamName = Name
type alias TypeName = Name

type ID = ID Int
type alias Cursor = Maybe ID
deID (ID x) = x

type alias Pos = Mouse.Position
type alias MouseEvent = {pos: Mouse.Position, button: Int}
type alias Offset = {x: Int, y: Int, offsetCheck: Int}
type alias CanvasPos = {x: Int, y: Int, canvasPosCheck : Int}

type Type = FunctionCall
          | FunctionDef
          | Datastore
          | Value
          | Page

type alias NodeDict = Dict Int Node
type alias Node = { name : Name
                  , id : ID
                  , pos : Pos
                  , tipe : Type
                  -- for DSes
                  , fields : List (FieldName, TypeName)
                  -- for functions
                  , parameters : List ParamName
                  }

type alias Edge = { source : ID
                  , target : ID
                  , param : ParamName
                  }

type alias LeftButton = Bool
type Msg
    = ClearCursor Mouse.Position
    | NodeClick Node
    | RecordClick Mouse.Position
    | DragNodeStart Node MouseEvent
    | DragNodeMove ID Offset Mouse.Position
    | DragNodeEnd ID Mouse.Position
    | DragSlotStart Node ParamName MouseEvent
    | DragSlotMove Mouse.Position
    | DragSlotEnd Node
    | DragSlotStop Mouse.Position
    | ReplInputMsg String
    | ReplSubmitMsg
    | EntryInputMsg String
    | EntrySubmitMsg
    | KeyPress Keyboard.KeyCode
    | CheckEscape Keyboard.KeyCode
    | FocusResult (Result Dom.Error ())
    | RPCCallBack (List RPC) (Result Http.Error (NodeDict, List Edge, Maybe ID))

type RPC
    = LoadInitialGraph
    | AddDatastore Name Pos
    | AddDatastoreField ID FieldName TypeName
    | AddFunctionCall Name Pos (List ID)
    | AddAnon Pos
    | AddValue String Pos
    | UpdateNodePosition ID -- no pos cause it's in the node
    | AddEdge ID (ID, ParamName)
    | DeleteNode ID
    | ClearEdges ID
    | RemoveLastField ID

-- Values that we serialize
type alias Model = { nodes : NodeDict
                   , edges : List Edge
                   , errors : List String
                   , dragPos : Pos
                   , drag : Drag
                   -- these values are serialized via Editor
                   , tempFieldName : FieldName
                   , cursor : Cursor
                   , focused : Bool
                   , prevNode : Maybe ID
                   , entryPos : Pos
                   , clickPos : Pos
                   , replValue : String
                   , entryValue : String
                   }

type alias Editor = { cursor : Maybe Int
                    , focused : Bool
                    , entryPos : Pos
                    , clickPos : Pos
                    , entryValue : String
                    , replValue : String
                    , prevNode : Maybe Int
                    , tempFieldName : FieldName
                    }

type Hole = NoHole
          | ResultHole Node
          | ParamHole Node String Int

type Drag = NoDrag
          | DragNode ID Offset -- offset between the click and the node pos
          | DragSlot ID ParamName Mouse.Position -- starting point of edge
