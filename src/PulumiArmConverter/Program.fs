module Program

open System
open Bicep.Core
open Bicep.Core.FileSystem
open Foundatio.Storage
open Converter
open System.IO
open Pulumi.Experimental.Converter
open Pulumi.Codegen
open Bicep.Decompiler

let errorResponse (message: string) = 
    let diagnostics = ResizeArray [
        Diagnostic(Summary=message, Severity=DiagnosticSeverity.Error)
    ]
    
    ConvertProgramResponse(Diagnostics=diagnostics)

let convertProgram (request: ConvertProgramRequest): ConvertProgramResponse = 
    let armFile =
       request.Args
       |> Seq.pairwise
       |> Seq.tryFind (fun (argKey, argValue) -> argKey = "--entry")
       |> Option.map (fun (_, entry) ->
           if not (entry.EndsWith ".json")
           then entry
           else entry + ".json")
       |> Option.map (fun entryArm ->
           if Path.IsPathRooted(entryArm)
           then entryArm
           else Path.Combine(request.SourceDirectory, entryArm))
       |> Option.orElse (
           Directory.EnumerateFiles(request.SourceDirectory)
           |> Seq.tryFind (fun file -> Path.GetExtension(file) = ".json")
       )

    match armFile with
    | None -> 
        errorResponse "Could not find the entry ARM file from the source directory"
    | Some entryArmFile ->
        let content = File.ReadAllText entryArmFile
        let workspace = Workspaces.Workspace()
        let fs = System.IO.Abstractions.FileSystem()
        let fileResolver = FileResolver(fs)
        let entryBicepFile = Path.ChangeExtension(entryArmFile, ".bicep")
        let bicepUri =  $"file:///{entryBicepFile}"
        let decompilerOptions = DecompileOptions()
        let decompileResult = TemplateConverter.DecompileTemplate(workspace, fileResolver, Uri bicepUri, content, decompilerOptions)
        let storageOptions = FolderFileStorageOptions(Folder=request.SourceDirectory)
        let storage = new FolderFileStorage(storageOptions)
        let program = fst(decompileResult.ToTuple())

        let result = Compile.compileProgramWithComponents {
            entryBicepSource = Compile.BicepSource.Program program
            sourceDirectory = request.SourceDirectory
            targetDirectory = request.TargetDirectory
            storage = storage
        }

        match result with
        | Error error -> errorResponse error
        | Ok() -> ConvertProgramResponse.Empty

convertProgram
|> Converter.CreateSimple
|> Converter.Serve
|> Async.AwaitTask
|> Async.RunSynchronously