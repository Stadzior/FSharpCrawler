// Learn more about F# at http://fsharp.org

open System
open FSharp.Data
open System.IO
open System.Text.RegularExpressions
open UrlHelpers

//let countWords =words |> Seq.countBy(fun x -> x)

[<EntryPoint>]
let main argv =     
    let containsUrlToken = Array.contains("-url")

    let urls = 
        if (containsUrlToken(argv)) then
            let urlAttributeIndex = argv |> Seq.findIndex(fun x -> String.Equals(x, "-url"))
            argv.[urlAttributeIndex + 1].Split(',')
            |> Array.filter(fun x -> Regex.IsMatch(x, UrlHelpers.baseHostUrlPattern))
        else
            Array.empty<string>

    if (Array.isEmpty(urls)) then
        Console.WriteLine("No valid urls provided.")
        0
    else
        let shouldWriteToConsole = Array.contains("-console")
        let shouldWriteToFile = Array.contains("-file")
        let filePath = 
            if shouldWriteToFile(argv) then
                    let fileAttributeIndex = argv |> Seq.findIndex(fun x -> String.Equals(x, "-file"))
                    __SOURCE_DIRECTORY__ + "\\" + argv.[fileAttributeIndex + 1]
            else ""

        let shouldRetrieveLinks = Array.contains("-a")
        let shouldRetrieveImages = Array.contains("-img")
        let shouldRetrieveScripts = Array.contains("-script")
        let shouldRetrieveTexts = Array.contains("-text")

        let documents = Array.map(fun x -> HtmlDocument.Load(x))

        let words =
            [ 
                if shouldRetrieveLinks(argv) then 
                    documents 
                    |> Array.map(fun x -> yield linkWords(documents))
                if shouldRetrieveImages(argv) then yield imageWords(documents)
                if shouldRetrieveScripts(argv) then yield scriptWords(documents)
                if shouldRetrieveTexts(argv) then yield textWords(documents)
            ]
            |> Seq.concat
            |> Seq.map(fun x -> (string x).Replace('!',' ').Replace('?',' ').Replace('.',' ').Trim().ToLower())
            |> Seq.filter(fun x -> not(String.IsNullOrWhiteSpace(x)) && Regex.Match(x,"\w").Success)
        

        if shouldWriteToConsole(argv) then
            for word in words |> Seq.countBy(fun x -> x) |> Seq.sortBy(fun x -> x) do
                Console.WriteLine(word)

        if shouldWriteToFile(argv) then
            if File.Exists(filePath) then
                File.Delete(filePath);
            File.AppendAllLines(filePath, words 
                |> Seq.countBy(fun x -> x)
                |> Seq.map(fun x -> x.ToString()))
    
        0 // return an integer exit code
