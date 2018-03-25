module UrlHelpers
open System
open System.Text.RegularExpressions

// (?i) - case insensitive mode
// [\w]+ - one or more word or '-' chars
// \. - one dot
// \w{2,3} - two to three word chars 
// (\.\w{2})? - once or none dot and two word chars 
// e.g. aa.com, aa-aa.com.pl, aaaaaaa.co.uk
let baseHostUrlPattern = "(?i)[\w-]+\.\w{2,3}(\.\w{2})?"

// (?i) - case insensitive mode
// (^https?|^http?):// - one or none occurence of https:// or http://
// (^www.?)? - one or none occurence of www.
// \/(.)* - / and any char
//e.g. http://aaa.com, https://
let fullUrlPattern = "(?i)((^https?|^http?)://)?(^www.?)?" + baseHostUrlPattern + "\/(.)*"

// (?i) - case insensitive mode
// \/ - /
// ([\w\-]+\/)+ - one or more occurence of word char with successing /
// [\w\-]+\.[\w]{1,4} - the very end of relative path (e.g. example.html)
// e.g. /aaa/bb/c-c/ddd.html
let relativeUrl = "(?i)\/([\w\-]+\/)+[\w\-]+\.[\w]{1,4}"

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
