﻿module UrlHelpers
open System
open System.Text.RegularExpressions

// e.g. aa.com, aa-aa.com.pl, aaaaaaa.co.uk
let baseHostUrlPattern = "(?i)^[\w\-\,\„\”\!]*(\.\w([\-\w\,\„\”\!]*\w)*)*\.\w{2,3}[\/]?$"
// same as above but removes "exact match" constraint
let softBaseHostUrlPattern = "(?i)[\w\-\,\„\”\!]*(\.\w([\-\w\,\„\”\!]*\w)*)*\.\w{2,3}[\/]?"

// e.g. /aaa/bb/c-c/ddd.html
let relativeUrlPattern = "(?i)^([\/]?[\w\-\,\„\”\!]+)+(\.[\w]{1,4})?[\/]?$"

let fullUrlPattern = "(?i)^((https|http)://)?(www\.)?\w[\w\-\,\„\”\!]*(\.\w([\-\w\,\„\”\!]*\w)*)*\.\w{2,3}[\/]?$"

let normalizeUrl (inputUrl : string) =
    let urlTemp = Uri.TryCreate(inputUrl, UriKind.Absolute)
    let url = if not(fst(urlTemp)) then
                    Uri.TryCreate(inputUrl.Replace("www.",""), UriKind.Absolute)
                else
                    urlTemp

    let uri =
        match url with
        | true, str -> Some str
        | _ ->  let url'' = Uri.TryCreate("http://" + inputUrl, UriKind.Absolute)
                match url'' with
                | true, str -> Some str
                | _ -> None
    match uri with
    | Some x -> let host = x.Host.Replace("www.","")
                let path = x.AbsolutePath
                let host' = Regex(baseHostUrlPattern, RegexOptions.RightToLeft).Match(host).Value
                let pattern = "(?i)^https?://((www\.)|([^\.]+\.))" + Regex.Escape(host') + "[^\"]*"
                let m = Regex(pattern).IsMatch(string x)
                match m with
                | true -> "http://" + host + path
                | false -> "http://www." + host + path
    | None -> ""

let transformRelativeToFullUrl (inputUrl : string, baseUrl : string) =
    if (inputUrl.StartsWith('/') || baseUrl.EndsWith('/')) then
        normalizeUrl(baseUrl + inputUrl)
    else
        normalizeUrl(baseUrl + "/" + inputUrl)

let getNormalizedBaseUrl (inputUrl : string) =
    normalizeUrl(Regex.Match(inputUrl, softBaseHostUrlPattern).Value)

let getExplorableUrls (urls : seq<string>, baseUrl : string) = 
    urls
        |> Seq.map(fun x -> x.Replace("%20","").Replace(" ", ""))
                                |> Seq.map(fun x -> 
                                    if (x.Contains("?")) then
                                        x.Substring(0, x.IndexOf('?'))
                                    else
                                        x)                             
                                |> Seq.filter(fun x -> not(String.IsNullOrWhiteSpace(x) || x.Contains("mailto") || x.Contains("#") || x.EndsWith(".pdf")))
                                |> Seq.map(fun x -> 
                                    if Regex.IsMatch(x, relativeUrlPattern) then
                                        transformRelativeToFullUrl(x, baseUrl)
                                    else
                                        x)
                                |> Seq.distinct