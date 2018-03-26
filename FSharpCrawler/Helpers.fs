module Helpers

open FSharp.Data
open System
open System.Net
open System.Text.RegularExpressions
open UrlHelpers

// e.g. aaaa-aaa'a
let wordPattern = "(^[\w]$)|(^[\w](\w|\-|\')*[\w]$)"

let getAttrOrEmptyStr (elem: HtmlNode) attr =
    match elem.TryGetAttribute(attr) with
    | Some v -> v.Value()
    | None -> ""

let getAllWordsFromUrl (url : string) =
    HtmlDocument.Load(url).Descendants["body"]
        |> Seq.map(fun x -> x.InnerText().Split(' '))
        |> Seq.concat
        |> Seq.map(fun x -> x.Trim().ToLower())
        |> Seq.filter(fun x -> Regex.IsMatch(x, wordPattern))

let getLinksFromUrl (includeExternal : bool, url : string) =
    HtmlDocument.Load(url).Descendants["a"]
        |> Seq.map (fun x -> 
            x.TryGetAttribute("href")
                |> Option.map (fun a -> a.Value()))
        |> Seq.filter (fun x -> includeExternal || Regex.IsMatch(x.Value, UrlHelpers.relativeUrlPattern) || Regex.Match(url, UrlHelpers.fullUrlPattern).Value.Equals(Regex.Match(x.Value, UrlHelpers.fullUrlPattern).Value))

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