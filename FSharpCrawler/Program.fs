// Learn more about F# at http://fsharp.org

open System
open FSharp.Data

let (|?) = defaultArg

let results = HtmlDocument.Load("http://ekobiobud.pl/")

let links = 
    results.Descendants ["a"]
    |> Seq.choose (fun x -> 
           x.TryGetAttribute("href")
           |> Option.map (fun a -> x.InnerText(), a.Value())
    )

let images = 
    results.Descendants ["img"]
    |> Seq.map (fun x -> 
        x.TryGetAttribute("src").Value.Value(),
        x.TryGetAttribute("alt").Value.Value()
    )

let scripts =
    results.Descendants ["script"]
    |> Seq.map(fun x ->        
        x.TryGetAttribute("src").Value.Value(),
        x.TryGetAttribute("type").Value.Value()
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
//let searchResults =
//    links
//    |> Seq.filter (fun (name, url) -> 
//                    name <> "Cached" && name <> "Similar" && url.StartsWith("/url?"))
//    |> Seq.map (fun (name, url) -> name, url.Substring(0, url.IndexOf("&sa=")).Replace("/url?q=", ""))
//    |> Seq.toArray

[<EntryPoint>]
let main argv =
    for link in links do
        Console.WriteLine("LINK: inner text: {0}, href: {1}", fst link, snd link)

    for image in images do         
        Console.WriteLine("IMAGE: src: {0}", image)

    //for script in scripts do         
    //    Console.WriteLine("SCRIPT: src: {0}\n", script)

    for text in texts do         
        Console.WriteLine("TEXT: {0}", text)
    Console.ReadKey() |> ignore
    0 // return an integer exit code
