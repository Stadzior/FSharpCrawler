module Helpers

open FSharp.Data
open System
open System.Net


let getAttrOrEmptyStr (elem: HtmlNode) attr =
    match elem.TryGetAttribute(attr) with
    | Some v -> v.Value()
    | None -> ""

let getAllWordsFromUrl (url : string) =
    HtmlDocument.Load(url).Descendants["body"]
        |> Seq.map (fun x -> x.InnerText())

let getLinksFromUrl (includeExternal : bool, url : string) =
    HtmlDocument.Load(url).Descendants ["a"]
        |> Seq.map (fun x -> 
            x.TryGetAttribute("href")
                |> Option.map (fun a -> x.InnerText(), a.Value()))
        |> Seq.filter(fun x -> includeExternal || Regex.IsMatch())
        |> Seq.concat

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