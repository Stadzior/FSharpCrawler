// Learn more about F# at http://fsharp.org

open System
open FSharp.Data
open System.IO
open System.Text.RegularExpressions
open UrlHelpers
open System

//let countWords =words |> Seq.countBy(fun x -> x)

[<EntryPoint>]
let main argv =     
    let containsUrlToken = Array.contains("-url")

    let urls = 
        if (containsUrlToken(argv)) then
            let urlAttributeIndex = argv |> Seq.findIndex(fun x -> String.Equals(x, "-url"))
            argv.[urlAttributeIndex + 1].Split(',')
            |> Array.filter(fun x -> Regex.IsMatch(x, UrlHelpers.baseHostUrlPattern))
            |> Array.map(fun x -> UrlHelpers.normalizeUrl(x))
            |> Array.distinct
        else
            Array.empty<string>

    if (Array.isEmpty(urls)) then
        Console.WriteLine("No valid urls provided.")
        0
    else
        let tags = argv |> Seq.filter(fun x -> Regex.IsMatch(x, "\-[\w]*"))  

        let filePath = 
            if tags |> Seq.contains("-file") then
                    let fileAttributeIndex = argv |> Seq.findIndex(fun x -> String.Equals(x, "-file"))
                    __SOURCE_DIRECTORY__ + "\\" + argv.[fileAttributeIndex + 1]
            else ""   

        let words = 
            urls
            |> Seq.map(fun x -> Helpers.getAllWordsFromUrl(x))
            |> Seq.concat

        let links =
            urls
            |> Seq.map(fun x -> Helpers.getLinksFromUrl((tags |> Seq.contains("-inclext")), x))
            |> Seq.concat
            |> Seq.map(fun x ->
                match x with
                | Some x -> x
                | None -> "")
            |> Seq.filter(fun x -> not(String.IsNullOrWhiteSpace(x)))

        if tags |> Seq.contains("-console") then
            if tags |> Seq.contains("-text") then
                for word in words |> Seq.countBy(fun x -> x) |> Seq.sortBy(fun x -> x) do
                    Console.WriteLine(word)
            if tags |> Seq.contains("-a") then
                for link in links do
                    Console.WriteLine(link)

        if tags |> Seq.contains("-file") then
            if File.Exists(filePath) then
                File.Delete(filePath)
            if tags |> Seq.contains("-text") then
                File.AppendAllLines(filePath, words 
                    |> Seq.countBy(fun x -> x)
                    |> Seq.map(fun x -> x.ToString()))
            if tags |> Seq.contains("-a") then
                File.AppendAllLines(filePath, links)
    
        0 // return an integer exit code
