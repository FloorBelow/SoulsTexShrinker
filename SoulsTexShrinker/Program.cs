using System;
using System.IO;
using SoulsFormats;
using DirectXTexNet;
using System.Runtime.InteropServices;

namespace SoulsTexShrinker {
    class Program {
        static void Main(string[] args) {
            foreach (string path in Directory.EnumerateFiles(@"E:\Extracted\Souls\Elden Ring\chr", "*_h.texbnd.dcx"))
                DownsizeTexbnd(path, @"E:\Extracted\Souls\Elden Ring\MODENGINENEW\texmod\chr");
        }

        static void DownsizeTexbnd(string path, string outputFolder, int levels = 1) {
            Console.WriteLine('\n' + Path.GetFileName(path));
            BND4 texbnd = BND4.Read(path);
            foreach (var file in texbnd.Files) {
                if (!file.Name.EndsWith(".tpf")) continue;
                TPF tpf = TPF.Read(file.Bytes);
                foreach (var texture in tpf.Textures) {
                    GCHandle pinnedMem = GCHandle.Alloc(texture.Bytes, GCHandleType.Pinned);
                    IntPtr address = pinnedMem.AddrOfPinnedObject();
                    var data = TexHelper.Instance.GetMetadataFromDDSMemory(address, texture.Bytes.Length, DDS_FLAGS.NONE);

                    if (data.Depth > 1 || data.ArraySize > 1 || data.Dimension != TEX_DIMENSION.TEXTURE2D | data.MipLevels <= levels) continue;
                    int oldWidth = data.Width;

                    var tex = TexHelper.Instance.LoadFromDDSMemory(address, texture.Bytes.Length, DDS_FLAGS.NONE);

                    Image[] images = new Image[tex.GetImageCount() - levels];
                    for (int i = 0; i < images.Length; i++) images[i] = tex.GetImage(i + levels);


                    data.Width /= 1 << levels;
                    data.Height /= 1 << levels;
                    data.MipLevels -= levels;


                    var newTex = TexHelper.Instance.InitializeTemporary(images, data);

                    using (Stream stream = newTex.SaveToDDSMemory(DDS_FLAGS.NONE)) {
                        using (MemoryStream memoryStream = new MemoryStream()) {
                            stream.CopyTo(memoryStream);
                            texture.Bytes = memoryStream.ToArray();
                        }
                    }

                    Console.WriteLine($"{texture.Name} {oldWidth} -> {data.Width}");
                    pinnedMem.Free();
                }
                file.Bytes = tpf.Write();
            }
            texbnd.Write(Path.Combine(outputFolder, Path.GetFileName(path)));
        }
    }
}
