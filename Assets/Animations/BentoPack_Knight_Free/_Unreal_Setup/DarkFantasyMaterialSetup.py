"""Dark Fantasy character packs - Unreal Engine material setup (UE 5.x, Python Editor Script Plugin).

Unreal's FBX import creates the materials of these characters WITHOUT textures (the FBX only names the texture
files, and Unreal does not look for them in Textures/Unreal). This script does that step for you:

  1. imports every T_<Name>_<Part>_{BaseColor,Normal,ORM,Emission}.png it finds in the pack's Textures/Common and
     Textures/Unreal folders (older packs: Textures/Unreal only), with the right settings (BaseColor/Emission sRGB; Normal = DirectX normal map; ORM linear masks:
     R occlusion, G roughness, B metallic);
  2. creates one master material /Game/DarkFantasy/M_DarkFantasy_Master and one instance per part;
  3. assigns them to every skeletal or static mesh in the project whose material slot is named M_<Name>_<Part>.

Run it AFTER importing the FBX files: Edit > Plugins > enable "Python Editor Script Plugin" (restart), then
File > Execute Python Script... and pick this file (keep it inside the pack, next to the character folders, or set
DF_TEXTURE_DIRS to the Textures/Common and Textures/Unreal folder(s), separated by ';'). Running it twice is safe: it
reuses what exists. A Weapons folder does not repeat the character's own textures (Textures/SHARED_FROM_CHARACTER.txt
lists them): import the hero's Character folder too, keep it in the same pack, and the script finds them there.
"""
import glob
import os
import struct
import zlib

import unreal

ROOT_PATH = "/Game/DarkFantasy"
EMISSION_STRENGTH = 2.5
MAPS = ("BaseColor", "Normal", "ORM", "Emission")
SHARED_NOTE = "SHARED_FROM_CHARACTER.txt"
tools = unreal.AssetToolsHelpers.get_asset_tools()
eal = unreal.EditorAssetLibrary
mel = unreal.MaterialEditingLibrary


def say(msg):
    unreal.log_warning("[DarkFantasy] " + msg)


def texture_dirs():
    env = os.environ.get("DF_TEXTURE_DIRS")
    if env:
        return [d for d in env.split(";") if os.path.isdir(d)]
    try:
        here = os.path.dirname(os.path.abspath(__file__))
    except NameError:
        return []
    for base in (os.path.dirname(here), here):
        found = sorted(set(os.path.normpath(p) for sub in ("Common", "Unreal")
                           for p in glob.glob(os.path.join(base, "**", "Textures", sub), recursive=True)))
        if found:
            return found
    return []


def shared_notes(dirs):
    """{T_<Name>_<Part>: folder of the Weapons package} from Textures/SHARED_FROM_CHARACTER.txt beside the texture folders."""
    out = {}
    for d in sorted(set(os.path.dirname(x) for x in dirs)):
        note = os.path.join(d, SHARED_NOTE)
        if os.path.isfile(note):
            for line in open(note, encoding="utf-8"):
                s = line.strip()
                if s.startswith("T_"):
                    out.setdefault(s, os.path.dirname(d))
    return out


def find_sets(dirs):
    """{(name, part): {map: file}} - the first folder that has a file wins (identical copies in every folder)."""
    sets = {}
    for d in dirs:
        for f in sorted(os.listdir(d)):
            stem, ext = os.path.splitext(f)
            bits = stem.split("_")
            if ext.lower() != ".png" or len(bits) < 4 or bits[0] != "T" or bits[-1] not in MAPS:
                continue
            key = (bits[1], "_".join(bits[2:-1]))
            sets.setdefault(key, {}).setdefault(bits[-1], os.path.join(d, f))
    return {k: v for k, v in sets.items() if "BaseColor" in v}


def black_png(path):
    raw = b"".join(b"\x00" + b"\x00\x00\x00" * 4 for _ in range(4))
    def chunk(t, d):
        return struct.pack(">I", len(d)) + t + d + struct.pack(">I", zlib.crc32(t + d) & 0xFFFFFFFF)
    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", 4, 4, 8, 2, 0, 0, 0)) + chunk(b"IDAT", zlib.compress(raw)) + chunk(b"IEND", b""))


def import_texture(path, dest, kind):
    name = os.path.splitext(os.path.basename(path))[0]
    asset = dest + "/" + name
    if not eal.does_asset_exist(asset):
        t = unreal.AssetImportTask()
        t.set_editor_property("filename", path)
        t.set_editor_property("destination_path", dest)
        t.set_editor_property("automated", True)
        t.set_editor_property("replace_existing", True)
        t.set_editor_property("save", False)
        tools.import_asset_tasks([t])
    tex = eal.load_asset(asset)
    if kind == "Normal":
        tex.set_editor_property("srgb", False)
        tex.set_editor_property("compression_settings", unreal.TextureCompressionSettings.TC_NORMALMAP)
        tex.set_editor_property("flip_green_channel", False)  # DirectX normal map: right as it is for Unreal
    elif kind == "ORM":
        tex.set_editor_property("srgb", False)
        tex.set_editor_property("compression_settings", unreal.TextureCompressionSettings.TC_MASKS)
    else:
        tex.set_editor_property("srgb", True)
        tex.set_editor_property("compression_settings", unreal.TextureCompressionSettings.TC_DEFAULT)
    eal.save_loaded_asset(tex, False)
    return tex


def sampler(mat, name, tex, stype, x, y):
    e = mel.create_material_expression(mat, unreal.MaterialExpressionTextureSampleParameter2D, x, y)
    e.set_editor_property("parameter_name", name)
    e.set_editor_property("texture", tex)
    e.set_editor_property("sampler_type", stype)
    return e


def master(defaults, black):
    path = ROOT_PATH + "/M_DarkFantasy_Master"
    if eal.does_asset_exist(path):
        return eal.load_asset(path)
    mat = tools.create_asset("M_DarkFantasy_Master", ROOT_PATH, unreal.Material, unreal.MaterialFactoryNew())
    st = unreal.MaterialSamplerType
    bc = sampler(mat, "BaseColor", defaults["BaseColor"], st.SAMPLERTYPE_COLOR, -800, -300)
    nm = sampler(mat, "Normal", defaults["Normal"], st.SAMPLERTYPE_NORMAL, -800, 0)
    orm = sampler(mat, "ORM", defaults["ORM"], st.SAMPLERTYPE_MASKS, -800, 300)
    em = sampler(mat, "Emission", black, st.SAMPLERTYPE_COLOR, -800, 600)
    k = mel.create_material_expression(mat, unreal.MaterialExpressionScalarParameter, -800, 900)
    k.set_editor_property("parameter_name", "EmissionStrength")
    k.set_editor_property("default_value", EMISSION_STRENGTH)
    mul = mel.create_material_expression(mat, unreal.MaterialExpressionMultiply, -400, 700)
    mp = unreal.MaterialProperty
    mel.connect_material_property(bc, "RGB", mp.MP_BASE_COLOR)
    mel.connect_material_property(nm, "RGB", mp.MP_NORMAL)
    mel.connect_material_property(orm, "R", mp.MP_AMBIENT_OCCLUSION)
    mel.connect_material_property(orm, "G", mp.MP_ROUGHNESS)
    mel.connect_material_property(orm, "B", mp.MP_METALLIC)
    mel.connect_material_expressions(em, "RGB", mul, "A")
    mel.connect_material_expressions(k, "", mul, "B")
    mel.connect_material_property(mul, "", mp.MP_EMISSIVE_COLOR)
    mel.recompile_material(mat)
    eal.save_loaded_asset(mat, False)
    return mat


def instance(name, part, texs, mat, black):
    mname = "MI_%s_%s" % (name, part)
    path = "%s/Materials/%s" % (ROOT_PATH, mname)
    mi = eal.load_asset(path) if eal.does_asset_exist(path) else tools.create_asset(
        mname, ROOT_PATH + "/Materials", unreal.MaterialInstanceConstant, unreal.MaterialInstanceConstantFactoryNew())
    mel.set_material_instance_parent(mi, mat)
    for m in MAPS:
        mel.set_material_instance_texture_parameter_value(mi, m, texs.get(m, black if m == "Emission" else None) or black)
    eal.save_loaded_asset(mi, False)
    return mi


def used_parts():
    """{(name, part)} named by a material slot of any skeletal or static mesh in the project."""
    used = set()
    for p in eal.list_assets("/Game", recursive=True, include_folder=False):
        cls = str(eal.find_asset_data(p).asset_class_path.asset_name)
        if cls not in ("SkeletalMesh", "StaticMesh"):
            continue
        a = eal.load_asset(p)
        slots = a.get_editor_property("materials") if cls == "SkeletalMesh" else a.get_editor_property("static_materials")
        for m in slots:
            s = str(m.get_editor_property("material_slot_name"))
            if s.startswith("M_"):
                used.add(tuple(s.split("_", 2)[1:]))
    return used


def main():
    dirs = texture_dirs()
    sets = find_sets(dirs)
    if not sets:
        say("no Textures/Common or Textures/Unreal folder found. Keep this script inside the pack, or set DF_TEXTURE_DIRS.")
        return
    notes = shared_notes(dirs)
    # 2026-11-08: a bundle holds many characters. Import only the texture sets of meshes that are in this project, so
    # a buyer who imports one hero does not get every hero's 4K textures. A pack with one character behaves as before.
    used = used_parts()
    for k in sorted(used):
        t = "T_%s_%s" % k
        if k not in sets and t in notes:
            say("M_%s_%s: the textures of %s are in the hero's Character folder, not in %s. Import that Character folder "
                "(keep it in the same pack), then run this script again." % (k[0], k[1], t, notes[t]))
    sets = {k: v for k, v in sets.items() if k in used}
    if not sets:
        say("no imported mesh has a material slot named M_<Name>_<Part> that matches the textures found. Import the FBX files first.")
        return
    say("texture folders: %d, parts: %s" % (len(dirs), ", ".join("%s_%s" % k for k in sorted(sets))))
    tmp = os.path.join(unreal.Paths.project_intermediate_dir(), "T_DarkFantasy_Black.png")
    black_png(tmp)
    black = import_texture(tmp, ROOT_PATH + "/Textures", "Emission")
    textures = {}
    for (name, part), files in sorted(sets.items()):
        textures[(name, part)] = {m: import_texture(f, "%s/Textures/%s" % (ROOT_PATH, name), m) for m, f in files.items()}
    first = next(iter(textures.values()))
    mat = master({m: first.get(m, black) for m in ("BaseColor", "Normal", "ORM")}, black)
    mis = {k: instance(k[0], k[1], v, mat, black) for k, v in textures.items()}
    slots_done = 0
    for p in eal.list_assets("/Game", recursive=True, include_folder=False):
        data = eal.find_asset_data(p)
        cls = str(data.asset_class_path.asset_name)
        if cls not in ("SkeletalMesh", "StaticMesh"):
            continue
        a = eal.load_asset(p)
        changed = False
        if cls == "SkeletalMesh":
            mats = a.get_editor_property("materials")
            for i in range(len(mats)):
                m = mats[i]  # a COPY of the struct: it must be written back into the array, or nothing changes
                s = str(m.get_editor_property("material_slot_name"))
                key = tuple(s.split("_", 2)[1:]) if s.startswith("M_") else None
                if key in mis:
                    m.set_editor_property("material_interface", mis[key])
                    mats[i] = m
                    changed = True
                    slots_done += 1
            if changed:
                a.set_editor_property("materials", mats)
        else:
            mats = a.get_editor_property("static_materials")
            for i, m in enumerate(mats):
                s = str(m.get_editor_property("material_slot_name"))
                key = tuple(s.split("_", 2)[1:]) if s.startswith("M_") else None
                if key in mis:
                    a.set_material(i, mis[key])
                    changed = True
                    slots_done += 1
        if changed:
            eal.save_loaded_asset(a, False)
    say("done: %d material instances, %d mesh material slots assigned" % (len(mis), slots_done))


main()
