module Passport.App

open System
open Giraffe

[<CLIMutable>]
type LoginModel =
    {
        redirectTo: string
    }

module Option =
    let ofResult = function
    | Ok v -> Some v
    | Error _ -> None

module Jwt =
    let create () =
        JWT.Builder.JwtBuilder()
            .WithAlgorithm(JWT.Algorithms.HMACSHA256Algorithm())
            .WithSecret("secret")
            .AddClaim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            .AddClaim("exp", DateTimeOffset.UtcNow.AddDays(7.0).ToUnixTimeSeconds())
            .AddClaim("handle", "tony-stark")
            .Build()

let login : HttpHandler =
    fun next ctx ->
        let redirect = 
            ctx.TryBindQueryString<LoginModel>()
            |> Option.ofResult
            |> Option.map (fun x -> x.redirectTo)
            |> Option.defaultValue "/id/dashboard"

        (
            setHttpHeader "Set-Cookie" <| sprintf "PsJwt-local=%s" (Jwt.create ())
            >=> redirectTo false redirect
        ) next ctx

let echoPath : HttpHandler =
    fun next ctx ->
        let path = string ctx.Request.Path
        text ("404 " + path) next ctx

let webApp : HttpHandler =
    choose [
        GET >=>
            choose [
                routex "/?" >=> login
                route "/dashboard" >=> redirectTo false "/library"
            ]
        setStatusCode 404 >=> echoPath
    ]
