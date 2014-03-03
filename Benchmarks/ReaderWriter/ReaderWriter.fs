﻿module ReaderWriter

// Inspired by http://t0yv0.blogspot.com/2011/12/making-async-5x-faster.html

open System
open System.Diagnostics
open Hopac
open Hopac.Job.Infixes

module Literal =
  let run n =
    printf "Literal: "
    let timer = Stopwatch.StartNew ()
    let i =
      run <| job {
        let ch = Ch.Now.create ()
        let rec writer i = job {
          if i = 0 then
            return! Ch.give ch 0
          else
            do! Ch.give ch i
            return! writer (i-1)
        }
        let rec reader sum = job {
          let! x = Ch.take ch
          if x = 0 then
            return sum
          else
            return! reader (sum + x)
        }
        do! Job.start (writer n)
        return! reader 0
      }
    let d = timer.Elapsed
    printf "%f hops per second\n" (float n / d.TotalSeconds)

module Tweaked =
  let run n =
    printf "Tweaked: "
    let timer = Stopwatch.StartNew ()
    let i =
      run <| job {
        let ch = Ch.Now.create ()
        let rec writer i =
          Ch.give ch i >>= fun () ->
          if i = 0 then
            Job.unit
          else
            writer (i-1)
        let rec reader sum =
          Ch.take ch >>= fun x ->
          if x = 0 then
            Job.result sum
          else
            reader (sum + x)
        do! Job.start (writer n)
        return! reader 0
      }
    let d = timer.Elapsed
    printf "%f hops per second\n" (float n / d.TotalSeconds)

let cleanup () =
  for i=1 to 10 do
    GC.Collect ()
    Threading.Thread.Sleep 50

do Literal.run 2000 ; cleanup ()
   Literal.run 20000 ; cleanup ()
   Literal.run 200000 ; cleanup ()
   Literal.run 2000000 ; cleanup ()
   Literal.run 20000000 ; cleanup ()

   Tweaked.run 2000 ; cleanup ()
   Tweaked.run 20000 ; cleanup ()
   Tweaked.run 200000 ; cleanup ()
   Tweaked.run 2000000 ; cleanup ()
   Tweaked.run 20000000 ; cleanup ()

