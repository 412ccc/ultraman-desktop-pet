# SPDX-License-Identifier: MIT
"""Build and test offline Windows executables with the system C# compiler."""
import hashlib
import os
from pathlib import Path
import subprocess
from zipfile import ZIP_DEFLATED, ZipFile

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
VERSION = "1.0.1"


def main():
    if os.name != "nt":
        raise SystemExit("Build this target on Windows with .NET Framework 4.x installed.")
    if ROOT.drive.upper() == "C:":
        raise SystemExit("Build from a folder on D: or another non-C: drive.")
    windows = Path(os.environ.get("WINDIR", r"C:\Windows"))
    compilers = [windows / folder / "v4.0.30319" / "csc.exe" for folder in
                 ("Microsoft.NET/Framework64", "Microsoft.NET/Framework")]
    compiler = next((path for path in compilers if path.is_file()), None)
    if compiler is None:
        raise SystemExit("The Windows .NET Framework C# compiler was not found.")
    build = ROOT / "build" / "windows"
    output = ROOT / "dist"
    build.mkdir(parents=True, exist_ok=True)
    output.mkdir(parents=True, exist_ok=True)
    temporary = build / "tmp"
    temporary.mkdir(parents=True, exist_ok=True)
    os.environ["TEMP"] = os.environ["TMP"] = str(temporary)
    with Image.open(ROOT / "assets" / "ultraman-spritesheet.png") as atlas:
        hero = atlas.crop((0, 0, 192, 208))
        hero.save(build / "hero.png")
        # Technical conversion of the existing artwork for Windows executable icons.
        icon = Image.new("RGBA", (256, 256))
        icon.paste(hero, ((256 - 192) // 2, (256 - 208) // 2))
        icon.save(build / "pet.ico", sizes=[(16, 16), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
    common = [str(compiler), "/nologo", "/codepage:65001", "/optimize+", "/platform:anycpu",
              "/reference:System.dll", "/reference:System.Core.dll", "/reference:System.Drawing.dll",
              "/reference:System.Windows.Forms.dll"]
    gui = ["/target:winexe", "/win32manifest:" + str(ROOT / "windows" / "app.manifest"),
           "/win32icon:" + str(build / "pet.ico")]
    pet = build / "UltramanPet.exe"
    setup = build / "UltramanPetSetup.exe"
    subprocess.run(common + gui + ["/out:" + str(pet),
                   "/resource:" + str(ROOT / "assets" / "ultraman-spritesheet.png") + ",PetAtlas",
                   str(ROOT / "windows" / "Storage.cs"), str(ROOT / "windows" / "Pet.cs")], check=True)
    resources = [(pet, "PetExe"), (ROOT / "LICENSE", "CodeLicense"),
                 (ROOT / "ASSET_TERMS.md", "AssetTerms"), (ROOT / "windows" / "USER_GUIDE.txt", "UserGuide"),
                 (build / "hero.png", "Hero")]
    subprocess.run(common + gui + ["/out:" + str(setup)] +
                   ["/resource:" + str(path) + "," + name for path, name in resources] +
                   [str(ROOT / "windows" / "Storage.cs"), str(ROOT / "windows" / "Setup.cs")], check=True)
    tests = build / "WindowsTests.exe"
    subprocess.run(common + ["/target:exe", "/out:" + str(tests), "/reference:" + str(pet),
                   "/reference:" + str(setup), str(ROOT / "windows" / "Tests.cs")], check=True)
    subprocess.run([str(tests), str(build)], check=True)
    (build / "UltramanPetPreview.exe").write_bytes(pet.read_bytes())
    setup_output = output / f"UltramanPet-Setup-{VERSION}.exe"
    setup_output.write_bytes(setup.read_bytes())
    portable = output / f"UltramanPet-Portable-{VERSION}.zip"
    with ZipFile(portable, "w", ZIP_DEFLATED) as archive:
        for source, destination in [(pet, "UltramanPet.exe"), (ROOT / "LICENSE", "LICENSE.txt"),
                                    (ROOT / "ASSET_TERMS.md", "ASSET_TERMS.txt"),
                                    (ROOT / "windows" / "USER_GUIDE.txt", "README.txt")]:
            archive.write(source, "UltramanPet/" + destination)
    with ZipFile(portable) as archive:
        assert archive.testzip() is None
        assert archive.read("UltramanPet/UltramanPet.exe") == pet.read_bytes()
    checksums = output / "SHA256SUMS.txt"
    checksums.write_text("".join(hashlib.sha256(path.read_bytes()).hexdigest() + "  " + path.name + "\n"
                                 for path in (setup_output, portable)), encoding="ascii")
    for path in (setup_output, portable, checksums):
        print(f"Built {path.name}: {path.stat().st_size:,} bytes")


if __name__ == "__main__":
    main()
