﻿module Helpers

open FSharp.Data
open System
open System.Net
open System.Text.RegularExpressions
open UrlHelpers

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
        |> Seq.filter (fun x -> includeExternal || Regex.IsMatch(x, UrlHelpers.relativeUrlPattern) || Regex.Match(fst(urlNodeTuple), UrlHelpers.fullUrlPattern).Value.Equals(Regex.Match(x, UrlHelpers.fullUrlPattern).Value))

let tryGetBodyFromUrl(url : string) : HtmlNode option =
    try
        HtmlDocument.Load(url).TryGetBody()
    with
        | :? WebException as _ex -> 
                try
                    HtmlDocument.Load(url.Replace("www.","")).TryGetBody()
                with
                    | :? WebException as _ex -> None

let rec getLinksFromNodeWithDepth(includeExternal : bool, urlNodeTuple : string * HtmlNode, depth : int) =
    if (depth < 1) then
        getLinksFromNode(includeExternal, urlNodeTuple)
    else
        getLinksFromNode(includeExternal, urlNodeTuple)
            |> Seq.map(fun x -> match tryGetBodyFromUrl(x) with
                                 | Some y -> (x,y)
                                 | None -> ("",Unchecked.defaultof<HtmlNode>))
            |> Seq.filter(fun x -> not(String.IsNullOrWhiteSpace(fst(x))))
            |> Seq.collect(fun x -> getLinksFromNodeWithDepth(includeExternal, x, depth - 1))

let mergeSeq<'T>(sequence1 : seq<'T>, sequence2 : seq<'T>) =
        seq [sequence1;sequence2]
        |> Seq.concat