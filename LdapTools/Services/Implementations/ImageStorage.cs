using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace LdapTools.Services.Implementations
{
    public class ImageStorage
    {
        public async Task<string> AddImageAsync(IFormFile file)
        {
            string ext = Path.GetExtension(file.FileName);
            string fileName = Guid.NewGuid().ToString() + ext;

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using (var image = Image.Load(stream))
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(300, 300),
                        Mode = ResizeMode.Max
                    }));

                    var outputPath = Path.Combine("wwwroot/images", fileName);
                    var fullPath = Path.GetFullPath(outputPath);

                    await image.SaveAsync(fullPath);
                }
            }

            return fileName;
        }

        public void DeleteImage(string fileName)
        {
            string image = "wwwroot/images/" + fileName;
            File.Delete(image);
        }
    }
}
