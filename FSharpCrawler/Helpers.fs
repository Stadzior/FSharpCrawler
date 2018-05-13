module Helpers

open FSharp.Data
open System
open System.Net
open System.Text.RegularExpressions
open UrlHelpers
open System.IO
open System.Drawing
open System.Threading.Tasks
open System.Threading

// e.g. aaaa-aaa'a
let wordPattern = "(^[\w]$)|(^[\w](\w|\-|\')*[\w]$)"

let getAttrOrEmptyStr (elem : HtmlNode) attr =
    match elem.TryGetAttribute(attr) with
    | Some v -> v.Value()
    | None -> ""

let getAllWordsFromNode (node : HtmlNode) =
    node.Descendants()
        |> Seq.filter(fun x -> x.Descendants() |> Seq.isEmpty)
        |> Seq.map(fun x -> x.InnerText().Split(' '))
        |> Seq.concat
        |> Seq.map(fun x -> x.Trim().ToLower())
        |> Seq.filter(fun x -> Regex.IsMatch(x, wordPattern))

let getLinksFromNode (includeExternal : bool, includeInternal : bool, urlNodeTuple : string * HtmlNode) =
    snd(urlNodeTuple).Descendants["a"]
        |> Seq.choose(fun x -> 
            x.TryGetAttribute("href")
                |> Option.map(fun x -> x.Value()))
        |> Seq.filter (fun x ->
                (includeExternal ||
                    Regex.IsMatch(x, UrlHelpers.relativeUrlPattern) ||
                    Regex.Match(fst(urlNodeTuple), UrlHelpers.fullUrlPattern).Value.Equals(Regex.Match(x, UrlHelpers.softFullUrlPattern).Value)))
        |> Seq.filter (fun x ->
                (includeInternal ||
                    not(Regex.IsMatch(x, UrlHelpers.relativeUrlPattern)) ||
                    not(Regex.Match(fst(urlNodeTuple), UrlHelpers.fullUrlPattern).Value.Equals(Regex.Match(x, UrlHelpers.softFullUrlPattern).Value))))
        |> Seq.distinct

type Microsoft.FSharp.Control.Async with
    static member AwaitTask (t : Task<'T>, timeout : int) =
        async {
            use cts = new CancellationTokenSource()
            use timer = Task.Delay (timeout, cts.Token)
            let! completed = Async.AwaitTask <| Task.WhenAny(t, timer)
            if completed = (t :> Task) then
                cts.Cancel ()
                let! result = Async.AwaitTask t
                return Some result
            else return None
        }

let tryGetBodyFromUrlAsyncWithTimeout(url : string, timeout : int) : string * HtmlNode option =
     (url, match (Async.AwaitTask(HtmlDocument.AsyncLoad(url) |> Async.StartAsTask, timeout) |> Async.RunSynchronously) with                
                | Some x -> x.TryGetBody()
                | _ -> None)

let tryGetBodyFromUrl(url : string) : string * HtmlNode option =
    try
        tryGetBodyFromUrlAsyncWithTimeout(url, 3000)
    with
        | :? WebException as _ex -> 
                try
                    tryGetBodyFromUrlAsyncWithTimeout(url.Replace("www.",""), 3000)
                with
                    | :? WebException as _ex -> (url.Replace("www.",""), None)
                    | :? UriFormatException as _ex -> (url.Replace("www.",""), None)
        | :? AggregateException as _ex ->        
                try
                    tryGetBodyFromUrlAsyncWithTimeout(url.Replace("www.",""), 3000)
                with
                    | :? WebException as _ex -> (url.Replace("www.",""), None)
                    | :? UriFormatException as _ex -> (url.Replace("www.",""), None)
                    | :? AggregateException as _ex -> (url.Replace("www.",""), None)
        | :? UriFormatException as _ex -> (url, None)        
        | :? NotSupportedException as _ex -> (url, None)
        | :? ArgumentException as _ex -> (url, None)
        | :? FileNotFoundException as _ex -> (url, None)
        | :? DirectoryNotFoundException as _ex -> (url, None)
        | :? IOException as _ex -> (url, None)
        | :? CookieException as _ex -> (url, None)

let mergeSeq<'T>(sequence1 : seq<'T>, sequence2 : seq<'T>) =
    seq [sequence1;sequence2]
    |> Seq.concat

let rec getLinksFromNodeWithDepth(includeExternal : bool, includeInternal : bool, urlNodeTuple : string * HtmlNode, baseUrl : string, depth : int) =
    (if depth < 1 then
        getExplorableUrls(getLinksFromNode(includeExternal, includeInternal, urlNodeTuple), baseUrl)
    else
        let normalizedLinks = getExplorableUrls(getLinksFromNode(includeExternal, includeInternal, urlNodeTuple), baseUrl)
                                |> Seq.map(fun x -> tryGetBodyFromUrl(x))
                                |> Seq.toArray
        mergeSeq(normalizedLinks |> Seq.map(fun x -> fst(x)), normalizedLinks 
                                                                |> Seq.filter(fun x -> snd(x).IsSome)
                                                                |> Seq.collect(fun x -> getLinksFromNodeWithDepth(includeExternal, includeInternal, (fst(x), snd(x).Value), getNormalizedBaseUrl(fst(x)), depth - 1))))
            |> Seq.distinct

let rec getAllWordsFromNodeWithDepth(includeExternal : bool, urlNodeTuple : string * HtmlNode, baseUrl : string, depth : int) =
    if depth < 1 then
            getAllWordsFromNode(snd(urlNodeTuple))
    else
        getExplorableUrls(getLinksFromNode(includeExternal, true, urlNodeTuple), baseUrl)
            |> Seq.map(fun x -> tryGetBodyFromUrl(x))
            |> Seq.filter(fun x -> snd(x).IsSome)
            |> Seq.map(fun x -> (fst(x), snd(x).Value))
            |> Seq.collect(fun x -> getAllWordsFromNodeWithDepth(includeExternal, x, getNormalizedBaseUrl(fst(x)), depth - 1))

let seqWithZerosOnDiff(left : seq<string * int>, right : seq<string * int>) =
    mergeSeq(left, right 
                    |> Seq.filter(fun x -> not(left |> Seq.exists(fun y -> fst(y).Equals(fst(x)))))
                    |> Seq.map(fun x -> fst(x), 0))

let calculateCosineSimilarity(left : seq<string * int>, right : seq<string * int>) = 
    let leftWithZeros = seqWithZerosOnDiff(left, right) 
                                    |> Seq.sortBy(fun x -> fst(x))
                                    |> Seq.map(fun x -> snd(x) |> float) 
                                    |> Seq.toArray

    let rightWithZeros = seqWithZerosOnDiff(right, left)
                                    |> Seq.sortBy(fun x -> fst(x))
                                    |> Seq.map(fun x -> snd(x) |> float) 
                                    |> Seq.toArray
    
    System.Math.Round(1.0 - Accord.Math.Distance.Cosine(leftWithZeros, rightWithZeros), 5)

let rec getNetMap(startingPoint : string * HtmlNode, depth : int) =
    (if depth < 1 then
        [|(fst(startingPoint), Seq.empty<string>)|]
    else
        let normalizedLinks = getExplorableUrls(getLinksFromNode(true, false, startingPoint), getNormalizedBaseUrl(fst(startingPoint)))
                                |> Seq.map(fun x -> Regex.Match(x, UrlHelpers.softFullUrlPattern).Value)
                                |> Seq.distinct
                                |> Seq.filter(fun x -> not(String.IsNullOrWhiteSpace(x) || Regex.Match(fst(startingPoint), UrlHelpers.fullUrlPattern).Value.Equals(x)))
                                |> Seq.map(fun x -> tryGetBodyFromUrl(x))
                                |> Seq.filter(fun x -> snd(x).IsSome)
                                |> Seq.map(fun x -> (fst(x), match snd(x) with
                                                        | Some x -> x
                                                        | None -> Unchecked.defaultof<HtmlNode>))
                                |> Seq.toArray
        let subNetMaps = [|[|(fst(startingPoint), normalizedLinks |> Seq.map(fun x -> fst(x)))|]; normalizedLinks |> Array.collect(fun x -> getNetMap(x, depth - 1))|] 
                            |> Array.collect(fun x -> x) 
        subNetMaps
            |> Array.map(fun x -> fst(x))
            |> Array.distinct
            |> Array.map(fun x -> (x, subNetMaps 
                                        |> Seq.filter(fun y -> fst(y).Equals(x))
                                        |> Seq.collect(fun y -> snd(y))
                                        |> Seq.distinct)))



let drawNetMap(map : (string * seq<string>)[], sizeX : int , sizeY : int, pointSize : int) = 
    let rnd = System.Random()
    let image = new Bitmap(sizeX, sizeY)
    let graphics = System.Drawing.Graphics.FromImage(image)
    let aliceBluePen = new Pen(Color.AliceBlue, float32(3))
    let redPen = new Pen(Color.Red, float32(3))
    let sitePoints = map |> Array.distinct |> Array.map(fun x -> (fst(x), (rnd.Next(pointSize, sizeX-pointSize), (rnd.Next(pointSize, sizeY-pointSize)))))
    for sitePoint in sitePoints do
        graphics.DrawEllipse(aliceBluePen, fst(snd(sitePoint)), snd(snd(sitePoint)), pointSize, pointSize)
        graphics.DrawString(fst(sitePoint), new Font("Arial", float32(16)), new SolidBrush(Color.AliceBlue), float32(fst(snd(sitePoint))), float32(snd(snd(sitePoint))))
        let links = snd(map |> Array.find(fun x -> fst(x).Equals(fst(sitePoint)))) |> Seq.toArray
        let linkPoints = sitePoints |> Array.filter(fun x -> links |> Array.contains(fst(x)))
        for linkPoint in linkPoints do
            if fst(linkPoint).Equals(fst(sitePoint)) then
                graphics.DrawEllipse(redPen, fst(snd(sitePoint)) + (pointSize/4), snd(snd(sitePoint)) + (pointSize/4), (pointSize/4), (pointSize/4))
            else
                graphics.DrawLine(aliceBluePen, fst(snd(sitePoint)) + (pointSize/2), snd(snd(sitePoint)) + (pointSize/2), fst(snd(linkPoint)) + (pointSize/2), snd(snd(linkPoint)) + (pointSize/2))
    image

let getFilePath(tags : string[], argv: string[]) = 
    let filePath = 
        if tags |> Seq.contains("-file") then
                let fileAttributeIndex = argv |> Seq.findIndex(fun x -> String.Equals(x, "-file"))
                __SOURCE_DIRECTORY__ + "\\" + argv.[fileAttributeIndex + 1]
        else "" 
    if File.Exists(filePath) then
        File.Delete(filePath)
    filePath

let findInMapByUrl(url : string, map : (string * seq<string>) []) =
    map |> Array.find(fun x -> fst(x).Equals(url))

let getLinksCount(url : string, map : (string * seq<string>) []) = 
    let count = snd(findInMapByUrl(url, map)) |> Seq.length
    if count > 0 then
        count
    else
        map.Length

let rec getPageRank(url : string, map : (string * seq<string>) [], alpha : float) =
    let firstThingy = (1.0-alpha)/float(map.Length)
    let minorPageRanksTuples = 
           snd(findInMapByUrl(url, map)) |> Seq.map(fun x -> (getPageRank(x, map, alpha), float(getLinksCount(x, map)))) |> Seq.toArray
    let sumOfPageRanks = (minorPageRanksTuples |> Array.sumBy(fun x -> fst(x)/snd(x)))
    let secondThingy = 
        if sumOfPageRanks > 0.0 then
            alpha * sumOfPageRanks
        else
            alpha
    firstThingy + secondThingy
    
