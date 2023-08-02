module Program

open System
open System.Threading
open Bicep.Core
open Bicep.Core.FileSystem
open Foundatio.Storage
open Pulumirpc
open Converter
open System.IO
open PulumiConverterPlugin
open Bicep.Decompiler

let errorResponse (message: string) = 
    let response = ConvertProgramResponse()
    let errorDiagnostic = Codegen.Diagnostic()
    errorDiagnostic.Summary <- message
    errorDiagnostic.Severity <- Codegen.DiagnosticSeverity.DiagError
    response.Diagnostics.Add(errorDiagnostic)
    response

let emptyResponse() = ConvertProgramResponse()

let convertProgram (request: ConvertProgramRequest) = task {
    let armFile =
       Directory.EnumerateFiles(request.SourceDirectory)
       |> Seq.tryFind (fun file -> Path.GetExtension(file) = ".json")

    match armFile with
    | None -> 
        return errorResponse "No ARM file found in the source directory"
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
        | Error error -> return errorResponse error
        | Ok() -> return emptyResponse()
}

let convertState (request: ConvertStateRequest) = task {
    let response = ConvertStateResponse()
    return response
}

type BicepConverterService() = 
    inherit Converter.ConverterBase()
    override _.ConvertProgram(request, ctx) = convertProgram(request)
    override _.ConvertState(request, ctx) = convertState(request)

let serve args =
    let cancellationToken = CancellationToken()
    PulumiConverterPlugin.Serve<BicepConverterService>(args, cancellationToken, System.Console.Out)
    |> Async.AwaitTask
    |> Async.RunSynchronously

[<EntryPoint>]
let main(args: string[]) =
    serve args
    0