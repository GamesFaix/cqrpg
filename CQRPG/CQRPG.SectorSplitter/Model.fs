module Model

open SixLabors.ImageSharp
open SixLabors.ImageSharp.PixelFormats

type SplitterResult<'data> = Result<'data, string>

type Image = Image<Rgba32>