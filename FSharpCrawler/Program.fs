// Learn more about F# at http://fsharp.org

open System
open FSharp.Data
open System.IO
open System.Text.RegularExpressions
open ActivePatterns
open Helpers
open UrlHelpers

//let countWords =words |> Seq.countBy(fun x -> x)

[<EntryPoint>]
let main argv =     
    let urls = 
        if argv |> Array.contains("-url") then
            let urlTagIndex = argv |> Seq.findIndex(fun x -> String.Equals(x, "-url"))
            argv.[urlTagIndex + 1].Split(',')
                |> Array.filter(fun x -> Regex.IsMatch(x, UrlHelpers.baseHostUrlPattern))
                |> Array.map(fun x -> UrlHelpers.normalizeUrl(x))
                |> Array.distinct
        else
            Array.empty<string>

    if (Array.isEmpty(urls)) then
        Console.WriteLine("No valid urls provided.")
        0
    else
        let tags = argv |> Seq.filter(fun x -> Regex.IsMatch(x, "\-[\w]*")) |> Seq.toArray

        let bodies = urls |> Seq.map(fun x -> Helpers.tryGetBodyFromUrl(x)) |> Seq.toArray

        if bodies |> Seq.exists(fun x -> snd(x).IsNone) then
            Console.WriteLine("Following urls are impossible to reach (incorrect url?) or lacks body tag (not a proper html file?):")
            bodies 
                |> Seq.filter(fun x -> snd(x).IsNone) 
                |> Seq.iter(fun x -> Console.WriteLine(fst(x)))
            Console.WriteLine("Proceeding with reachable ones...")

        let depth = 
            if tags |> Seq.contains("-depth") then                
                let depthTagIndex = argv |> Seq.findIndex(fun x -> String.Equals(x, "-depth"))
                match argv.[depthTagIndex + 1] with
                    | Int i -> i
                    | _ -> Console.WriteLine("Invalid depth value it should be an integer, proceeding with 0.");0;
            else
                0
        
        Console.WriteLine("Selected depth: " + depth.ToString())

        let reachableBodies = 
            bodies 
                |> Seq.filter(fun x -> snd(x).IsSome)
                |> Seq.map(fun x -> (fst(x), match snd(x) with
                                                | Some x -> x
                                                | None -> Unchecked.defaultof<HtmlNode>))
                |> Seq.toArray

        let links =
            mergeSeq(reachableBodies |> Seq.map(fun x -> fst(x)), reachableBodies 
                                                                    |> Seq.collect(fun x -> getLinksFromNodeWithDepth(tags |> Seq.contains("-inclext"), x, getNormalizedBaseUrl(fst(x)), depth)))

        let reachableBodiesWithDepth =
                 links
                    |> Seq.map(fun x -> tryGetBodyFromUrl(x))
                    |> Seq.filter(fun x -> snd(x).IsSome)
                    |> Seq.map(fun x ->
                                    match snd(x) with
                                        | Some y -> (fst(x), y)
                                        | None -> ("", Unchecked.defaultof<HtmlNode>))

        let words = 
            reachableBodiesWithDepth
                |> Seq.map(fun x -> Helpers.getAllWordsFromNode(snd(x)))
                |> Seq.concat

        if tags |> Seq.contains("-console") then
            if tags |> Seq.contains("-text") then
                for word in words 
                        |> Seq.countBy(fun x -> x) 
                        |> Seq.sortBy(fun x -> x) do
                    Console.WriteLine(word)
            if tags |> Seq.contains("-a") then
                for link in links do
                    Console.WriteLine(link)

        if tags |> Seq.contains("-file") then
            let filePath = 
                if tags |> Seq.contains("-file") then
                        let fileAttributeIndex = argv |> Seq.findIndex(fun x -> String.Equals(x, "-file"))
                        __SOURCE_DIRECTORY__ + "\\" + argv.[fileAttributeIndex + 1]
                else "" 
            if File.Exists(filePath) then
                File.Delete(filePath)
            if tags |> Seq.contains("-text") then
                File.AppendAllLines(filePath, words 
                    |> Seq.countBy(fun x -> x)
                    |> Seq.map(fun x -> x.ToString()))
            if tags |> Seq.contains("-a") then
                File.AppendAllLines(filePath, links)
    
        0 // return an integer exit code
