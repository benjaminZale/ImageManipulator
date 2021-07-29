#pragma warning disable IDE0051 // Remove unused private members
namespace ImageManipulator
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using McMaster.Extensions.CommandLineUtils;
    using SkiaSharp;
    using TagLib.Image;
    using File = System.IO.File;

#pragma warning disable SA1600 // Elements should be documented

    [Command(Name = "imageManipulator", Description = " An application to modify the orientation of an image.")]
    [HelpOption("-?")]
    internal class Program
    {
        [Option(Description = "Action to take", ShortName = "a")]
        [Required]
        private ActionKind Action { get; }

        [Argument(0, Description = "The input image.")]
        [Required]
        private string Input { get; }

        [Argument(1, Description = "The result of the action.")]
        [Required]
        private string Output { get; }

        [Option(Description = "Keep any metdata tags.", ShortName ="t")]
        private bool KeepTags { get; }

#pragma warning restore SA1600 // Elements should be documented

        /// <summary>
        /// The start of the application.
        /// </summary>
        /// <param name="args">See the properties for usage.</param>
        /// <returns>A error code.</returns>
        internal static async Task<int> Main(string[] args)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                return await CommandLineApplication.ExecuteAsync<Program>(args);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Encountered Exception:");
                Console.WriteLine($"{exception.GetType().Name}: {exception.Message}");
                Console.WriteLine(exception.StackTrace);
                return -500;
            }
            finally
            {
                Console.WriteLine($"Completed in {stopwatch.ElapsedMilliseconds}ms.");
            }
        }

        /// <summary>
        /// Executes the application after the properties are populated.
        /// </summary>
        /// <param name="cancellationToken">A token used to cancel the task.</param>
        /// <returns>The error code or 0 on success.</returns>
        private async Task<int> OnExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (!File.Exists(Input))
            {
                Console.WriteLine($"File '{Input}' was not found");
                return -1;
            }

            using Stream inputStream = File.OpenRead(Input);
            using SKCodec codec = SKCodec.Create(inputStream);
            using SKBitmap inputBitmap = SKBitmap.Decode(codec);

            if (inputBitmap == null)
            {
                Console.WriteLine($"Could not read '{Input}' because its not a valid image");
                return -2;
            }

            Action<SKCanvas> transfomation;
            int outputWidth;
            int outputheight;

            switch (Action)
            {
                case ActionKind.Clockwise:
                    outputWidth = inputBitmap.Height;
                    outputheight = inputBitmap.Width;
                    transfomation = (canvas) =>
                    {
                        canvas.RotateDegrees(90, 0, 0);
                        canvas.Translate(0, -outputWidth);
                    };
                    break;
                case ActionKind.CounterClockwise:
                    outputWidth = inputBitmap.Height;
                    outputheight = inputBitmap.Width;
                    transfomation = (canvas) =>
                    {
                        canvas.RotateDegrees(-90, 0, 0);
                        canvas.Translate(-outputheight, 0);
                    };
                    break;
                case ActionKind.Mirror:
                    outputWidth = inputBitmap.Width;
                    outputheight = inputBitmap.Height;
                    transfomation = (canvas) =>
                    {
                        canvas.Scale(-1, 1, outputWidth / 2, outputheight / 2);
                    };
                    break;
                case ActionKind.Flip:
                    outputWidth = inputBitmap.Width;
                    outputheight = inputBitmap.Height;
                    transfomation = (canvas) =>
                    {
                        canvas.Scale(1, -1, outputWidth / 2, outputheight / 2);
                    };
                    break;
                default:
                    throw new NotImplementedException();
            }

            // Generate the surface.
            SKImageInfo imageInfo = new SKImageInfo(outputWidth, outputheight, inputBitmap.Info.ColorType, inputBitmap.Info.AlphaType, inputBitmap.Info.ColorSpace);
            SKSurface skSurface = SKSurface.Create(imageInfo);

            // Apply the transform
            transfomation(skSurface.Canvas);
            skSurface.Canvas.DrawBitmap(inputBitmap, new SKPoint(0, 0));

            // Encode the image.
            SKImage snapshot = skSurface.Snapshot();
            SKData data = snapshot.Encode(codec.EncodedFormat, 100);

            Stream outputStream;

            if (!KeepTags)
            {
                outputStream = data.AsStream();
            }
            else
            {
                // Read the metadata
                inputStream.Seek(0, SeekOrigin.Begin);
                TagLib.File tfile = TagLib.File.Create(new StreamFileAbstraction(Input, inputStream));
                CombinedImageTag imageTags = tfile.Tag as CombinedImageTag;

                // Copy the metadata tags to the encoded image.
                await using Stream dataStream = data.AsStream();
                StreamFileAbstraction streamAbstraction = new StreamFileAbstraction(Output, dataStream);
                TagLib.File outfile = TagLib.File.Create(streamAbstraction);
                CombinedImageTag outTags = outfile.Tag as CombinedImageTag;
                foreach (var tag in imageTags.AllTags)
                {
                    TagLib.Tag createdTag = outfile.GetTag(tag.TagTypes, create: true);
                    tag.CopyTo(createdTag, overwrite: true);
                }

                outfile.Save();
                outputStream = streamAbstraction.ReadStream;
            }

            // Stream the data to the disk.
            await using FileStream fileStream = new FileStream(Output, FileMode.Create);
            await outputStream.CopyToAsync(fileStream, cancellationToken);
            await fileStream.FlushAsync(cancellationToken);
            return 0;
        }
    }
}
#pragma warning restore IDE0051 // Remove unused private members
