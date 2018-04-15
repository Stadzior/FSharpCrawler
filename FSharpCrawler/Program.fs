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
    let stopWatch = System.Diagnostics.Stopwatch.StartNew()
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
                |> Seq.sortBy(fun x -> fst(x)) 
                |> Seq.toArray

        let links =
            reachableBodies 
                |> Seq.map(fun x -> x, getLinksFromNodeWithDepth(tags |> Seq.contains("-inclext"), x, getNormalizedBaseUrl(fst(x)), depth)
                                            |> Seq.toArray)
            |> Seq.toArray

        let words = 
            reachableBodies 
                |> Seq.map(fun x -> x, getAllWordsFromNodeWithDepth(tags |> Seq.contains("-inclext"), x, getNormalizedBaseUrl(fst(x)), depth)
                                        |> Seq.countBy(fun x -> x)
                                        |> Seq.sortBy(fun x -> snd(x))
                                        |> Seq.toArray)
                |> Seq.toArray

        let reachableNotPermutatedYetBodies(body : string * HtmlNode) = 
            reachableBodies 
                |> Seq.filter(fun x -> (reachableBodies |> Seq.findIndex(fun y -> y.Equals(x))) > (reachableBodies |> Seq.findIndex(fun y -> y.Equals(body)))) 

        let perms = 
            seq {
                for body in reachableBodies do
                    for anotherBody in reachableNotPermutatedYetBodies(body) do
                        yield body,anotherBody

            } |> Seq.toArray           

        let cosineSimilarities = 
            perms 
                |> Seq.map(fun x -> "Cosine similiarity of " + fst(fst(x)) + " and " + fst(snd(x)), calculateCosineSimilarity(snd(words 
                                                                                                                            |> Seq.find(fun y -> fst(fst(y)).Equals(fst(fst(x))))), snd(words 
                                                                                                                                                                                        |> Seq.find(fun y -> fst(fst(y)).Equals(fst(snd(x)))))))
                |> Seq.toArray

        if tags |> Seq.contains("-console") then
            if tags |> Seq.contains("-text") then
                words
                    |> Seq.iter (fun x ->
                            Console.WriteLine("--------------------------------------" + fst(fst(x)) + "--------------------------------------")
                            for word in snd(x) do
                                Console.WriteLine(word))
            if tags |> Seq.contains("-a") then
                links
                    |> Seq.iter (fun x ->
                            Console.WriteLine("--------------------------------------" + fst(fst(x)) + "--------------------------------------")
                            for link in snd(x) do
                                Console.WriteLine(link))
            if tags |> Seq.contains("-cos") then
                cosineSimilarities
                    |> Seq.iter(fun x -> Console.WriteLine(x.ToString()))

        if tags |> Seq.contains("-file") then
            let filePath = 
                if tags |> Seq.contains("-file") then
                        let fileAttributeIndex = argv |> Seq.findIndex(fun x -> String.Equals(x, "-file"))
                        __SOURCE_DIRECTORY__ + "\\" + argv.[fileAttributeIndex + 1]
                else "" 
            if File.Exists(filePath) then
                File.Delete(filePath)
            if tags |> Seq.contains("-text") then
                words
                    |> Seq.iter (fun x ->
                            File.AppendAllLines(filePath, [| "--------------------------------------" + fst(fst(x)) + "--------------------------------------" |])
                            File.AppendAllLines(filePath, snd(x) |> Seq.map(fun x -> x.ToString())))
            if tags |> Seq.contains("-a") then
                links
                    |> Seq.iter(fun x ->
                            File.AppendAllLines(filePath, [| "--------------------------------------" + fst(fst(x)) + "--------------------------------------" |])
                            File.AppendAllLines(filePath, snd(x)))
            if tags |> Seq.contains("-cos") then
                File.AppendAllLines(filePath, cosineSimilarities |> Seq.map(fun x -> x.ToString()))

        stopWatch.Stop()
        if tags |> Seq.contains("-console") then
            Console.WriteLine("Execution time: " + stopWatch.Elapsed.Seconds.ToString() + "s")
        if tags |> Seq.contains("-file") then
            let filePath = 
                if tags |> Seq.contains("-file") then
                        let fileAttributeIndex = argv |> Seq.findIndex(fun x -> String.Equals(x, "-file"))
                        __SOURCE_DIRECTORY__ + "\\" + argv.[fileAttributeIndex + 1]
                else "" 
            File.AppendAllLines(filePath, ["Execution time: " + stopWatch.Elapsed.Seconds.ToString() + "s"])
        0 // return an integer exit code
