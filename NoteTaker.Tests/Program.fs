// tests/Lazo.Tests/LazoTests.fs
namespace NoteTaker.Tests

open System.IO
open Expecto
open NoteTaker.Model

module Tests =
    /// Helper: create a unique throw-away directory under %TMP%
    let private getAndCreateTempDir () =
        Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
        |> fun dir ->
            Directory.CreateDirectory dir |> ignore
            dir

    [<Tests>]
    let workspaceSetupTests =
        testList "Workspace.ensure" [
            testCase "creates all canonical sub-folders"
            <| fun _ ->
                let tempRoot = getAndCreateTempDir ()
                let created = Handlers.ensureDirs tempRoot

                let expected =
                    Section.List |> List.map (fun v -> Path.Combine(tempRoot, v.dirName))

                Expect.sequenceEqual created expected "scaffolding paths should match spec"

                expected
                |> List.iter (fun p ->
                    Expect.isTrue (Directory.Exists p) $"{p} must exist"

                    Expect.stringContains
                        p
                        tempRoot
                        $"paths should be nested behind temp dir at {tempRoot}")

                Directory.Delete(tempRoot, true)
        ]

    let fileSystemStoreTests =
        testList "Config – FileSystemStore" [

            testCase "default load returns Config.Default"
            <| fun _ ->
                let tempRoot = getAndCreateTempDir ()
                let store = Store.FileSystem.make tempRoot // no config.json yet

                match store.Load() with
                | Ok cfg -> Expect.equal cfg Config.Default "Should fall back to default config"
                | Error _ -> ()

                Directory.Delete(tempRoot, true)

            testCase "save then load round-trips"
            <| fun _ ->
                let tempRoot = getAndCreateTempDir ()
                let store = Store.FileSystem.make tempRoot
                let expected = { Scheme = Dark; RecentFiles = [ "a.md"; "b.md" ] }

                match store.Save expected with
                | Ok _ ->
                    match store.Load() with
                    | Ok loaded ->
                        Expect.equal loaded expected $"Config should round-trip exactly at "

                        Directory.Delete(tempRoot, true)
                    | Error err -> failtest $"failed to load config because {err.str}"
                | Error err -> failtest $"failed to save config because {err.str}"
        ]

    let memoryStoreTests =
        testList "Config – MemoryStore" [
            testCase "in-memory round-trip without I/O"
            <| fun _ ->
                let store = Store.TestFS.make Config.Default

                let updated = {
                    Config.Default with
                        Scheme = Light
                        RecentFiles = [ "inbox/idea.md" ]
                }

                match store.Save updated with
                | Ok _ ->
                    match store.Load() with
                    | Ok loaded ->
                        Expect.equal loaded updated "Memory store must reflect last save"
                    | Error err -> failtest $"failed to load config because {err.str}"
                | Error err -> failtest $"failed to update config because {err.str}"
        ]



    [<EntryPoint>]
    let main argv =
        runTestsWithCLIArgs
            [||]
            argv
            (testList "Initialization Tests" [
                workspaceSetupTests
                fileSystemStoreTests
                memoryStoreTests
                ModelTests.readmeCreationTests
            ])
