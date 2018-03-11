// Learn more about F# at http://fsharp.org

open System
open FSharp.Data

let results = HtmlDocument.Load("http://pduch.kis.p.lodz.pl/")

let links = 
    results.Descendants ["a"]
    |> Seq.choose (fun x -> 
           x.TryGetAttribute("href")
           |> Option.map (fun a -> x.InnerText(), a.Value())
    )

//let searchResults =
//    links
//    |> Seq.filter (fun (name, url) -> 
//                    name <> "Cached" && name <> "Similar" && url.StartsWith("/url?"))
//    |> Seq.map (fun (name, url) -> name, url.Substring(0, url.IndexOf("&sa=")).Replace("/url?q=", ""))
//    |> Seq.toArray

[<EntryPoint>]
let main argv =
    for link in links do
        Console.WriteLine("Caption: {0}\nLink: {1}\n", fst link, snd link)
    Console.ReadKey() |> ignore
    0 // return an integer exit code
