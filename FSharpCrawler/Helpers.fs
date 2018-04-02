module Helpers

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

let rec getAllLeafs (node : HtmlNode) : seq<HtmlNode> = 
    node.Descendants()
        |> Seq.collect(fun x -> getAllLeafs(x))

let getAllWordsFromNode (node : HtmlNode) =
    getAllLeafs(node)
        |> Seq.map(fun x -> x.InnerText().Split(' '))
        |> Seq.concat
        |> Seq.map(fun x -> x.Trim().ToLower())
        |> Seq.filter(fun x -> Regex.IsMatch(x, wordPattern))

let getLinksFromNode (includeExternal : bool, urlNodeTuple : string * HtmlNode) =
    snd(urlNodeTuple).Descendants["a"]
        |> Seq.map (fun x -> 
            x.TryGetAttribute("href")
                |> Option.map (fun a -> a.Value()))
        |> Seq.filter (fun x -> includeExternal || Regex.IsMatch(x.Value, UrlHelpers.relativeUrlPattern) || Regex.Match(fst(urlNodeTuple), UrlHelpers.fullUrlPattern).Value.Equals(Regex.Match(x.Value, UrlHelpers.fullUrlPattern).Value))

let tryGetBodyFromUrl(url : string) : HtmlNode option =
    try
        HtmlDocument.Load(url).TryGetBody()
    with
        | :? WebException as ex -> 
                try
                    HtmlDocument.Load(url.Replace("www.","")).TryGetBody()
                with
                    | :? WebException as ex -> None



//let linkWordsFromDocument (document : HtmlDocument) = 

//let linkWordsFromDocuments (documents : seq<HtmlDocument>) =
//    documents 
//        |> Seq.map(fun x -> linkWords(x))
//        |> Seq.concat

//let imageWords (document : HtmlDocument) = 
//    document.Descendants ["img"]
//        |> Seq.map (fun x -> (getAttrOrEmptyStr x "alt").Split(' '))
//        |> Seq.concat

//let scriptWords (document : HtmlDocument) =
//    document.Descendants ["script"]
//    |> Seq.map (fun x -> x.InnerText().Split(' '))
//    |> Seq.concat

//let textWords (document : HtmlDocument) =
//    document.Descendants["body"]
//    |> Seq.map(fun x -> x.InnerText().Split(' '))
//    |> Seq.concat
 
let mergeSeq<'T>(sequence1 : seq<'T>, sequence2 : seq<'T>) =
        seq [sequence1;sequence2]
        |> Seq.concat