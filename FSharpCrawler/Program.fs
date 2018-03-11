// Learn more about F# at http://fsharp.org

open System
open FSharp.Data

let getAttrOrEmptyStr (elem: HtmlNode) attr =
  match elem.TryGetAttribute(attr) with
  | Some v -> v.Value()
  | None -> ""

let results = HtmlDocument.Load("http://joemonster.org//");

let links = 
    results.Descendants ["a"]
    |> Seq.choose (fun x -> 
           x.TryGetAttribute("href")
           |> Option.map (fun a -> x.InnerText(), a.Value())
    )

let images = 
    results.Descendants ["img"]
    |> Seq.map (fun x ->
        getAttrOrEmptyStr x "src",
        getAttrOrEmptyStr x "alt"
    )

let scripts =
    results.Descendants ["script"]
    |> Seq.map (fun x ->
        getAttrOrEmptyStr x "src",
        getAttrOrEmptyStr x "type"
    )

let texts =
    results.Descendants ["p"]
    |> Seq.append(results.Descendants["h1"])
    |> Seq.append(results.Descendants["h2"])
    |> Seq.append(results.Descendants["h3"])
    |> Seq.append(results.Descendants["h4"])
    |> Seq.append(results.Descendants["h5"])
    |> Seq.append(results.Descendants["h6"])
    |> Seq.map(fun x -> x.InnerText())

[<EntryPoint>]
let main argv =

    //let lineSplitted = Console.ReadLine().Split(' ');

    for link in links do
        Console.WriteLine("LINK: inner text: {0}, href: {1}", fst link, snd link)

    for image in images do         
        Console.WriteLine("IMAGE: src: {0}, alt:{1}", fst image, snd image)

    for script in scripts do         
        Console.WriteLine("SCRIPT: src: {0}, type: {1}", fst script, snd script)

    for text in texts do         
        Console.WriteLine("TEXT: {0}", text)
    Console.ReadKey() |> ignore

    0 // return an integer exit code
