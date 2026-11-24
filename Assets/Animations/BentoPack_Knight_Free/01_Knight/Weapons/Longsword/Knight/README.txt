Dark Fantasy Knight - Longsword - version 2.0
by Bento Pack

CONTENTS
  FBX/SK_Knight.fbx          skinned character, bind pose, no animation
  FBX/Animations/<Category>/    7 animation clips, one FBX each (skeleton only)
  FBX/Props/                    SK_Knight_Longsword.fbx, SM_Knight_Longsword.fbx
  Textures/Common/              BaseColor (sRGB), used by both engines
  Textures/Unreal/              Normal (DirectX, green down), ORM (R occlusion, G roughness, B metallic)
  Textures/Unity/               Normal_OpenGL, MetallicSmoothness (RGB metallic, A smoothness), Occlusion
  NOT in this folder:            the textures of Body, Gauntlet, Helmet (the character itself). They are in
                                the hero's Character folder: import Character/Knight FIRST, in the same project.
                                Textures/SHARED_FROM_CHARACTER.txt lists them; the setup scripts read it.
  CLIPS.csv                     every clip with category, source, variant flags and length
  AI_DISCLOSURE.txt             how this asset was made

MESH
  Triangles: 517,488   Vertices: 263,167   Height: 1.85 m
  Meshes: Knight_Body, Knight_Gauntlet_L, Knight_Gauntlet_R, Knight_Helmet, Knight_Skirt, Knight_Undersuit
  Materials: M_Knight_Body, M_Knight_Gauntlet, M_Knight_Helmet
  Texture sets: Longsword
  Texture sets in the hero's Character folder (import it too): Body, Gauntlet, Helmet
  LODs: none included.

SKELETON
  70 bones. 61 use the Unreal mannequin bone NAMES (root, pelvis, spine_01-03, neck_01, head,
  clavicle/upperarm/lowerarm/hand, 15 finger bones per hand, upper/lower-arm and thigh/calf twist_01,
  thigh/calf/foot/ball). It is a 61-bone SUBSET, not the Epic skeleton asset: there are no spine_04/05,
  neck_02, metacarpal, IK (ik_foot_root, ik_hand_*) or corrective bones. Use it as its own skeleton and
  retarget with the IK Retargeter (Unreal) or the Humanoid avatar (Unity).
  Helper bones (9), keyed in every clip:
    pauldron_l     shoulder-plate hinge (child of clavicle); rotation baked into every clip
    weapon_r       weapon socket on the right hand; the skinned prop (SK_*) is bound to it
    pauldron_r     shoulder-plate hinge (child of clavicle); rotation baked into every clip
    fauld_fl       armour-skirt plate hinge (child of pelvis); rotation baked into every clip from the thigh motion
    fauld_fr       armour-skirt plate hinge (child of pelvis); rotation baked into every clip from the thigh motion
    fauld_sl       armour-skirt plate hinge (child of pelvis); rotation baked into every clip from the thigh motion
    fauld_sr       armour-skirt plate hinge (child of pelvis); rotation baked into every clip from the thigh motion
    fauld_bl       armour-skirt plate hinge (child of pelvis); rotation baked into every clip from the thigh motion
    fauld_br       armour-skirt plate hinge (child of pelvis); rotation baked into every clip from the thigh motion

ANIMATION
  7 clips, 30 fps, root motion on the `root` bone.
  Variants: *_sword = same motion with the weapon held on weapon_r.
  Clips per category: Combat_Longsword 7

SCALE AND AXES
  1 unit = 1 m (FBX scale applied). Up = +Y in the FBX; the character faces the FBX front axis and
  imports facing +Z in Unity. Pivot at the feet (ground level).

UNITY
  - Set Project Settings > Player > Color Space = LINEAR. In Gamma the steel renders as shiny plastic.
  - Humanoid: Unity's Humanoid retargeting DROPS every non-humanoid bone, so the helper bones above
    lose their keys. Import each clip with an AvatarMask that includes ALL transforms of the skeleton
    (Animation tab > Mask > Create From This Model), or import as Generic.
  - Materials: the FBX files do not find the textures on their own, so the character imports WHITE until this is done.
    Copy _Unity_Setup/Editor/DarkFantasyMaterialSetup.cs from the top of the pack ONCE into your project, then run
    Tools > Dark Fantasy > Set Up Materials. It handles every pack folder in the project, for URP and Built-in. HDRP is
    not supported: it needs a mask map the pack does not include.
    By hand: BaseColor (Textures/Common) as Base Map; Normal_OpenGL (Textures/Unity) as Normal Map; MetallicSmoothness
    as Metallic with Smoothness Source = Metallic Alpha; Occlusion. URP Lit or Built-in Standard.
  - This is a Weapons folder: import the hero's Character folder too (same project), or the character's
    materials stay untextured - the setup script names every part it cannot texture.
  - Animation Compression: set it to Off on clips where a hand holds a weapon. The default Keyframe Reduction
    moved joints by up to 13 mm in our test and the left hand drifted up to 1.2 cm along a two-handed shaft.
  - Humanoid: turn on Apply Root Motion, or Bake Into Pose on the root rotation and position. Without either
    a spin does not turn the body.
  - Use Generic for two-handed weapon clips. With compression Off, Generic stayed within 0.4 mm of the source
    on armed clips (2.5 mm on the others). Humanoid moved joints up to 12 mm and the left hand up to 3 cm along
    a two-handed shaft: add hand IK if you need Humanoid there.
  - Weapons: SK_<weapon>.fbx is skinned to the same skeleton, 100% on weapon_r. Point its SkinnedMeshRenderer
    at the character's bones and it follows every clip with no socket offset.

UNREAL ENGINE
  - Import SK_Knight.fbx as Skeletal Mesh (creates its own Skeleton), then the Animations with that
    Skeleton selected. Keep 'Import Uniform Scale' 1.0.
  - Materials: Unreal's FBX import makes the materials WITHOUT textures (the character imports grey).
    Enable Edit > Plugins > 'Python Editor Script Plugin' (restart), then File > Execute Python Script... and pick
    _Unreal_Setup/DarkFantasyMaterialSetup.py at the top of the pack.
    It imports Textures/Common and Textures/Unreal with the right settings, creates /Game/DarkFantasy/M_DarkFantasy_Master and one
    instance per part, and assigns them to every mesh slot named M_<Name>_<Part>. Safe to run again.
  - By hand instead: BaseColor (Textures/Common) sRGB; Normal = the DirectX map (Textures/Unreal, compression Normalmap);
    ORM with sRGB OFF (compression Masks), R = AO, G = Roughness, B = Metallic.
  - This is a Weapons folder: import the hero's Character folder too; the script names every part whose textures
    it cannot find (they live in Character/<Name>/Textures).

TESTED WITH
  Unity 6000.3.22f1 with URP 17.3: batch import of this hero's folders in the shared texture layout (a Weapons folder together with the hero's Character folder), clips 306/306 + 7/7, valid avatar, materials textured by the included setup script. Unreal Engine 5.8.4: first batch import into the test project, clips 306 + 7, 0 failures, joint positions against the source in Blender worst 0.061 mm raw and 3.684 mm with Unreal's default compression; the included Unreal setup script textured every material slot. Not tested: Play-in-Editor, a packaged game, HDRP, Humanoid retargeting.

SUPPORT
  pedro.bento@bentocorp.org
