// Learn more about F# at http://fsharp.org

open System
open FSharp.Data
open System.IO

let getAttrOrEmptyStr (elem: HtmlNode) attr =
  match elem.TryGetAttribute(attr) with
  | Some v -> v.Value()
  | None -> ""
  
let links (document : HtmlDocument) = 
    document.Descendants ["a"]
    |> Seq.choose (fun x -> 
           x.TryGetAttribute("href")
           |> Option.map (fun a -> x.InnerText(), a.Value())
    )

let images (document : HtmlDocument) = 
    document.Descendants ["img"]
    |> Seq.map (fun x ->
        getAttrOrEmptyStr x "src",
        getAttrOrEmptyStr x "alt"
    )

//let internalWriter (fileName: string, lines: string[]) =

//    for line in lines do
//        sw.WriteLine(line)

let scripts (document : HtmlDocument) =
    document.Descendants ["script"]
    |> Seq.map (fun x ->
        getAttrOrEmptyStr x "src",
        getAttrOrEmptyStr x "type"
    )

let texts (document : HtmlDocument) =
    document.Descendants ["p"]
    |> Seq.append(document.Descendants["h1"])
    |> Seq.append(document.Descendants["h2"])
    |> Seq.append(document.Descendants["h3"])
    |> Seq.append(document.Descendants["h4"])
    |> Seq.append(document.Descendants["h5"])
    |> Seq.append(document.Descendants["h6"])
    |> Seq.map(fun x -> x.InnerText())

let rec mainLoop() = 
    let lineSplitted = Console.ReadLine().Split(' ');
    let shouldQuit = Array.contains("q");

    if (shouldQuit(lineSplitted)) then
        Console.WriteLine("Quit requested...")
    else
        let document = HtmlDocument.Load(lineSplitted.[0]);
    
        let shouldWriteToConsole = Array.contains("-console")
        let shouldWriteToFile = Array.contains("-file")
        let fileName = 
            if shouldWriteToFile(lineSplitted) then
                if shouldWriteToConsole(lineSplitted) then
                    lineSplitted.[3] 
                else
                    lineSplitted.[4]
            else ""

        let shouldRetrieveLinks = Array.contains("-a")
        let shouldRetrieveImages = Array.contains("-img")
        let shouldRetrieveScripts = Array.contains("-script")
        let shouldRetrieveTexts = Array.contains("-text")

        if shouldRetrieveLinks(lineSplitted) then
            for link in links(document) do
                if shouldWriteToConsole(lineSplitted) then
                    Console.WriteLine("LINK: inner text: {0}, href: {1}", fst link, snd link)
                else ()
            //if shouldWriteToFile(lineSplitted) then
            //    internalWriter(fileName, links 
            //    |> Seq.choose(String.Format("LINK: inner text: {0}, href: {1}", fst link, snd link))
            //    |> Seq.toArray)
            //else ()
        else ()
            
        if shouldRetrieveImages(lineSplitted) then
            for image in images(document) do
                if shouldWriteToConsole(lineSplitted) then
                    Console.WriteLine("IMAGE: src: {0}, alt:{1}", fst image, snd image)
                else ()
            //if shouldWriteToFile(lineSplitted) then
            //    internalWriter(fileName, links 
            //    |> Seq.choose(String.Format("LINK: inner text: {0}, href: {1}", fst link, snd link))
            //    |> Seq.toArray)
            //else ()
        else ()

        if shouldRetrieveScripts(lineSplitted) then
            for script in scripts(document) do
                if shouldWriteToConsole(lineSplitted) then
                    Console.WriteLine("SCRIPT: src: {0}, type: {1}", fst script, snd script)
                else ()
            //if shouldWriteToFile(lineSplitted) then
            //    internalWriter(fileName, links 
            //    |> Seq.choose(String.Format("LINK: inner text: {0}, href: {1}", fst link, snd link))
            //    |> Seq.toArray)
            //else ()
        else ()
    
        if shouldRetrieveTexts(lineSplitted) then
            for text in texts(document) do
                if shouldWriteToConsole(lineSplitted) then
                    Console.WriteLine("TEXT: {0}", text)
                else ()
            //if shouldWriteToFile(lineSplitted) then
            //    internalWriter(fileName, links 
            //    |> Seq.choose(String.Format("LINK: inner text: {0}, href: {1}", fst link, snd link))
            //    |> Seq.toArray)
            //else ()
        else ()

        mainLoop()

[<EntryPoint>]
let rec main argv = 
    mainLoop()
    0 // return an integer exit code
