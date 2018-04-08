module UrlHelpers
open System
open System.Text.RegularExpressions

// e.g. aa.com, aa-aa.com.pl, aaaaaaa.co.uk
let baseHostUrlPattern = "(?i)^[\w\-]*(\.\w([\-\w]*\w)*)*\.\w{2,3}[\/]?$"

// e.g. /aaa/bb/c-c/ddd.html
let relativeUrlPattern = "(?i)^([\/]?[\w\-]+)+(\.[\w]{1,4})?[\/]?$"

let fullUrlPattern = "(?i)^((https|http)://)?(www\.)?\w[\w\-]*(\.\w([\-\w]*\w)*)*\.\w{2,3}[\/]?$"

let normalizeUrl (inputUrl : string) =
    let url = Uri.TryCreate(inputUrl, UriKind.Absolute)
    let uri =
        match url with
        | true, str -> Some str
        | _ ->  let url'' = Uri.TryCreate("http://" + inputUrl, UriKind.Absolute)
                match url'' with
                | true, str -> Some str
                | _ -> None
    match uri with
    | Some x -> let host = x.Host
                let path = x.AbsolutePath
                let host' = Regex(baseHostUrlPattern, RegexOptions.RightToLeft).Match(host).Value
                let pattern = "(?i)^https?://((www\.)|([^\.]+\.))" + Regex.Escape(host') + "[^\"]*"
                let m = Regex(pattern).IsMatch(string x)
                match m with
                | true -> "http://" + host + path
                | false -> "http://www." + host + path
    | None -> ""

let transformRelativeToFullUrl (inputUrl : string, baseUrl : string) =
    if (inputUrl.StartsWith('/')) then
        normalizeUrl(baseUrl + inputUrl)
    else
        normalizeUrl(baseUrl + "/" + inputUrl)