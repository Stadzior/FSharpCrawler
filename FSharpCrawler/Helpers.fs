module Helpers

open FSharp.Data
open System
open System.Net
open System.Text.RegularExpressions
open UrlHelpers
open System.IO
open System.Drawing

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

let getLinksFromNode (includeExternal : bool, urlNodeTuple : string * HtmlNode) =
    snd(urlNodeTuple).Descendants["a"]
        |> Seq.choose(fun x -> 
            x.TryGetAttribute("href")
                |> Option.map(fun x -> x.Value()))
        |> Seq.filter (fun x -> includeExternal || Regex.IsMatch(x, UrlHelpers.relativeUrlPattern) ||
                                                                            Regex.Match(fst(urlNodeTuple), UrlHelpers.fullUrlPattern).Value.Equals(Regex.Match(x, UrlHelpers.softFullUrlPattern).Value))
        |> Seq.distinct

let tryGetBodyFromUrl(url : string) : string * HtmlNode option =
    try
        (url, HtmlDocument.Load(url).TryGetBody())
    with
        | :? WebException as _ex -> 
                try
                    (url.Replace("www.",""), HtmlDocument.Load(url.Replace("www.","")).TryGetBody())
                with
                    | :? WebException as _ex -> (url.Replace("www.",""), None)
                    | :? UriFormatException as _ex -> (url.Replace("www.",""), None)
        | :? UriFormatException as _ex -> (url, None)        
        | :? NotSupportedException as _ex -> (url, None)
        | :? ArgumentException as _ex -> (url, None)
        | :? FileNotFoundException as _ex -> (url, None)
        | :? DirectoryNotFoundException as _ex -> (url, None)
        | :? IOException as _ex -> (url, None)

let mergeSeq<'T>(sequence1 : seq<'T>, sequence2 : seq<'T>) =
    seq [sequence1;sequence2]
    |> Seq.concat

let rec getLinksFromNodeWithDepth(includeExternal : bool, urlNodeTuple : string * HtmlNode, baseUrl : string, depth : int) =
    (if depth < 1 then
        getExplorableUrls(getLinksFromNode(includeExternal, urlNodeTuple), baseUrl)
    else
        let normalizedLinks = getExplorableUrls(getLinksFromNode(includeExternal, urlNodeTuple), baseUrl)
                                |> Seq.map(fun x -> tryGetBodyFromUrl(x))
                                |> Seq.toArray
        mergeSeq(normalizedLinks |> Seq.map(fun x -> fst(x)), normalizedLinks 
                                                                |> Seq.filter(fun x -> snd(x).IsSome)
                                                                |> Seq.collect(fun x -> getLinksFromNodeWithDepth(includeExternal, (fst(x), snd(x).Value), getNormalizedBaseUrl(fst(x)), depth - 1))))
            |> Seq.distinct

let rec getAllWordsFromNodeWithDepth(includeExternal : bool, urlNodeTuple : string * HtmlNode, baseUrl : string, depth : int) =
    if depth < 1 then
            getAllWordsFromNode(snd(urlNodeTuple))
    else
        getExplorableUrls(getLinksFromNode(includeExternal, urlNodeTuple), baseUrl)
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

//let generateSiteMap()

let drawSiteMap() = new Bitmap(100,100)

let rec getNetSize(url : string, depth : int) = 
    if (depth > 0) then
        let subnetsSizes = getLinksFromNode(true, tryGetBodyFromUrl(url))
                                |> Seq.map(fun x -> Regex.Match(x, UrlHelpers.softFullUrlPattern).Value)
                                |> Seq.distinct
                                |> Seq.filter(fun x -> not(Regex.Match(url, UrlHelpers.fullUrlPattern).Value.Equals(x)))
                                |> Seq.map(fun x -> getNetSize(x, depth - 1))
        let subnetsSum = subnetsSizes |> Seq.sum
        subnetsSum + 1
    else
        1

let rec getPageRank(url : string, alpha : double, depth : int) = 
    let minorPageRanks = getLinksFromNode(true, tryGetBodyFromUrl(url))
                            |> Seq.map(fun x -> Regex.Match(x, UrlHelpers.softFullUrlPattern).Value)
                            |> Seq.distinct
                            |> Seq.filter(fun x -> not(Regex.Match(url, UrlHelpers.fullUrlPattern).Value.Equals(x)))
                            |> Seq.map(fun x -> getPageRank(x, alpha, depth - 1))
                            |> Seq.toArray
    let netSize = 1 + minorPageRanks.Length + 
    (1.0 - alpha)/

    
