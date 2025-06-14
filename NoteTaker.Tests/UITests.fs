namespace NoteTaker.Tests

open Expecto
open NoteTaker.Editor

module TextEditorTests =
    let private startTextTest =
        testCase "start of text"
        <| fun _args ->
            let ln, col = computePosition "" 0
            Expect.equal (ln, col) (1, 1) "start should be at 1,1"

    let private movedCaretTest = testCase "middle of content" <| fun _args ->
        let ln, col = computePosition "foo\nbar" 7
        Expect.equal (ln, col) (2, 4) "Caret should be at col 4 of line 2"


    [<Tests>]
    let computePositionTests =
        testList "computePosition" [ startTextTest; movedCaretTest ]
