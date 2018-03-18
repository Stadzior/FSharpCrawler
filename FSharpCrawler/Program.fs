// Learn more about F# at http://fsharp.org

open System
open FSharp.Data
open System.IO

let getAttrOrEmptyStr (elem: HtmlNode) attr =
  match elem.TryGetAttribute(attr) with
  | Some v -> v.Value()
  | None -> ""
  
let linkWords (document : HtmlDocument) = 
    document.Descendants ["a"]
        |> Seq.map (fun x -> x.InnerText().Split(' '))
        |> Seq.concat

let imageWords (document : HtmlDocument) = 
    document.Descendants ["img"]
        |> Seq.map (fun x -> (getAttrOrEmptyStr x "alt").Split(' '))
        |> Seq.concat

let scriptWords (document : HtmlDocument) =
    document.Descendants ["script"]
    |> Seq.map (fun x -> x.InnerText().Split(' '))
    |> Seq.concat

let textWords (document : HtmlDocument) =
    document.Descendants ["p"]
    |> Seq.append(document.Descendants["h1"])
    |> Seq.append(document.Descendants["h2"])
    |> Seq.append(document.Descendants["h3"])
    |> Seq.append(document.Descendants["h4"])
    |> Seq.append(document.Descendants["h5"])
    |> Seq.append(document.Descendants["h6"])
    |> Seq.map(fun x -> x.InnerText().Split(' '))
    |> Seq.concat
 
let mergeSeq<'T>(sequence1 : seq<'T>, sequence2 : seq<'T>) =
        seq [sequence1;sequence2]
        |> Seq.concat

//let countWords =words |> Seq.countBy(fun x -> x)

[<EntryPoint>]
let main argv =     
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

    let document = HtmlDocument.Load(argv.[0]);
    let words =
        [ 
            if shouldRetrieveLinks(argv) then yield linkWords(document)
            if shouldRetrieveImages(argv) then yield imageWords(document)
            if shouldRetrieveScripts(argv) then yield scriptWords(document)
            if shouldRetrieveTexts(argv) then yield textWords(document)
        ]
        |> Seq.concat
        |> Seq.map(fun x -> (string x).Replace('!',' ').Replace('?',' ').Replace('.',' ').Trim().ToLower())
        |> Seq.filter(fun x -> not(String.IsNullOrWhiteSpace(x)))

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
