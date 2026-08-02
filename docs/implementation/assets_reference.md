# Third-Party Asset References

This document records the origin, licensing, project use, and local location of
third-party assets used by the feasibility prototype. It is an attribution
record, not a replacement for the original license terms.

The initial source and license links below were verified on 22 July 2026;
later additions record their verification or download date in the relevant
section. Retain a copy of each downloaded asset and its license information
with project records.

## Player

### Remy

- **Asset type:** Rigged character and Mixamo animations
- **Creator/provider:** Adobe Mixamo
- **Source:** [Remy in the Mixamo character library](https://www.mixamo.com/#/?page=1&query=&type=Character)
- **License guidance:** [Adobe Mixamo FAQ](https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html)
- **License summary:** Adobe permits Mixamo characters and animations to be used
  royalty-free in personal, commercial, and non-profit projects, including
  video games. The applicable Adobe terms remain authoritative.
- **Attribution requirement:** No attribution requirement is stated in the
  linked Mixamo FAQ.
- **Prototype use:** Player character with Idle, Walking, and Fast Run clips
- **Local location:** `assets/characters/player/`
- **Status:** In use
- **Reference note:** Mixamo uses a browser application rather than a stable
  public page for each character, so the supplied link opens its character
  library rather than a Remy-specific static page.

## Environment

### Stylized Nature MegaKit

- **Asset type:** Stylized environment and nature models
- **Creator/provider:** Quaternius
- **Source:** [Stylized Nature MegaKit](https://quaternius.com/packs/stylizednaturemegakit.html)
- **License:** [CC0 1.0 Universal](https://creativecommons.org/publicdomain/zero/1.0/)
- **Asset details:** 116 textured nature models supplied in FBX, OBJ, Blend, and
  glTF formats
- **Attribution requirement:** Not required; Quaternius is credited here for
  provenance.
- **Prototype use:** Environment dressing
- **Local location:** `assets/environment/nature/`
- **Status:** In use as a curated two-tree subset

### Old Russian House

- **Asset type:** Building model
- **Creator:** Yury Misiyuk (`@Tim0`)
- **Source:** [Old Russian house on Sketchfab](https://sketchfab.com/3d-models/old-russian-house-be5c284793b149d1bc3e359bd8c44cdc)
- **License:** [Creative Commons Attribution 4.0 International](https://creativecommons.org/licenses/by/4.0/)
- **Attribution requirement:** Credit the creator, asset title, source, license,
  and any modifications.
- **Credit text:** "Old Russian house" by Yury Misiyuk (`@Tim0`), licensed under
  CC BY 4.0, via Sketchfab. Modified for use in Ashwood County 3D Prototype.
- **Prototype use:** Retained as a candidate future replacement exterior
- **Local location:** `assets/environment/buildings/`
- **Status:** Retained but not currently instantiated; the modular prototype
  house uses project-owned geometry

### Forest Ground 01

- **Asset type:** Forest-floor ground texture
- **Creator:** Rob Tuytel
- **Source:** [Forest Ground 01 on Poly Haven](https://polyhaven.com/a/forrest_ground_01)
- **License:** [CC0 1.0 Universal](https://polyhaven.com/license)
- **Asset details:** Two-metre seamless surface; the project currently retains
  the 1K diffuse, roughness, and displacement maps.
- **Attribution requirement:** Not required; the author and source are credited
  here for provenance.
- **Prototype use:** Grass and forest-floor terrain surface
- **Local location:** `assets/third_party/materials/grass/`
- **Status:** In use

### Forest Ground 06

- **Asset type:** Compacted forest-soil texture
- **Creator:** Charlotte Baglioni
- **Source:** [Forest Ground 06 on Poly Haven](https://polyhaven.com/a/forest_ground_06)
- **License:** [CC0 1.0 Universal](https://polyhaven.com/license)
- **Asset details:** 2.1-metre seamless surface; the project retains the 1K
  diffuse and displacement maps.
- **Attribution requirement:** Not required; the author and source are credited
  here for provenance.
- **Prototype use:** Dirt roadside shoulders
- **Local location:** `assets/third_party/materials/grass/`
- **Status:** In use

### Modular Electricity Poles

- **Asset type:** Modular utility-pole model and material set
- **Creator:** James Ray Cock
- **Source:** [Modular Electricity Poles on Poly Haven](https://polyhaven.com/a/modular_electricity_poles)
- **License:** [CC0 1.0 Universal](https://polyhaven.com/license)
- **Attribution requirement:** Not required; the author and source are credited
  here for provenance.
- **Prototype use:** Textures applied to lightweight primitive utility-pole
  proxies; the original model geometry is not currently present in the project.
- **Local location:** `assets/third_party/common/poles/`
- **Status:** Partially integrated

### Weathered Roadsign | GameReady

- **Asset type:** Weathered speed-limit sign model
- **Creator:** Mark Peters (`mark-peters`)
- **Source:** [Weathered Roadsign on Sketchfab](https://sketchfab.com/3d-models/weathered-roadsign-gameready-d7020fa18b7b4220ac4eb2c49af4cddc)
- **License:** [Creative Commons Attribution 4.0 International](https://creativecommons.org/licenses/by/4.0/)
- **Attribution requirement:** Credit the creator, asset title, source, license,
  and any modifications.
- **Credit text:** "Weathered Roadsign | GameReady" by Mark Peters, licensed
  under CC BY 4.0, via Sketchfab. Repositioned for Ashwood County 3D Prototype.
- **Prototype use:** Roadside sign prop
- **Local location:** `assets/third_party/common/signs/`
- **Status:** In use

### Barrel & Crate

- **Asset type:** Low-poly container prop set
- **Creator:** DJMaesen (`@bumstrum`)
- **Source:** [Barrel & Crate on Sketchfab](https://sketchfab.com/3d-models/barrel-crate-ed9f90ec662b4b3c972b4567899b8293)
- **License:** [Creative Commons Attribution 4.0 International](https://creativecommons.org/licenses/by/4.0/)
- **Attribution requirement:** Credit the creator, asset title, source, license,
  and any modifications.
- **Credit text:** "Barrel & Crate" by DJMaesen (`@bumstrum`), licensed under
  CC BY 4.0, via Sketchfab. Scaled and selected for Ashwood County 3D Prototype.
- **Prototype use:** Intact barrel and crate beside the house
- **Local location:** `assets/third_party/common/containers/`
- **Status:** In use

### Old Tyre

- **Asset type:** Worn vehicle-tyre model and material set
- **Creator:** James Ray Cock
- **Source:** [Old Tyre on Poly Haven](https://polyhaven.com/a/old_tyre)
- **License:** [CC0 1.0 Universal](https://polyhaven.com/license)
- **Attribution requirement:** Not required; the author and source are credited
  here for provenance.
- **Prototype use:** Full authored 1K glTF model and PBR material set used for
  discarded tyres in the Main Street abandonment dressing.
- **Local location:**
  `assets/third_party/environment/main_street_dressing/poly_haven/old_tyre/`
- **Project wrapper:** `assets/environment/vehicles/old_tyre.tscn`
- **Status:** In use; replaces the former project-authored torus proxy

### Asphalt 02

- **Asset type:** Asphalt surface texture
- **Creator:** Rob Tuytel
- **Source:** [Asphalt 02 on Poly Haven](https://polyhaven.com/a/asphalt_02)
- **License:** [CC0 1.0 Universal](https://polyhaven.com/license)
- **Attribution requirement:** Not required; the author and source are credited
  here for provenance.
- **Prototype use:** Road surface
- **Local location:** `assets/third_party/materials/asphalt/`
- **Status:** In use

### Clean Asphalt

- **Asset type:** Asphalt surface texture
- **Creator:** Dimitrios Savva
- **Source:** [Clean Asphalt on Poly Haven](https://polyhaven.com/a/clean_asphalt)
- **License:** [CC0 1.0 Universal](https://polyhaven.com/license)
- **Attribution requirement:** Not required; the author and source are credited
  here for provenance.
- **Prototype use:** Road surface
- **Local location:** `assets/environment/roads/`
- **Status:** Planned for prototype use

## Main Street Visual Pass Assets

The following assets were downloaded or newly integrated for the Main Street
visual-quality pass on 28 July 2026.

### Red Brick, Factory Brick, and Painted Brick

- **Asset type:** Seamless PBR building-façade materials
- **Creator/provider:** Poly Haven
- **Sources:** [Red Brick](https://polyhaven.com/a/red_brick),
  [Factory Brick](https://polyhaven.com/a/factory_brick), and
  [Painted Brick](https://polyhaven.com/a/painted_brick)
- **License:** [CC0 1.0 Universal](https://polyhaven.com/license)
- **Asset details:** The project retains each material's 2K diffuse, OpenGL
  normal, and packed ARM maps.
- **Attribution requirement:** None; provider and sources are recorded for
  provenance.
- **Prototype use:** Shared project-owned triplanar materials on the modular
  Main Street storefront façades
- **Downloaded location:** `assets/third_party/materials/building_facades/poly_haven/`
- **Project materials:** `assets/materials/ashwood_main_street/`
- **Status:** In use

### Bushes

- **Asset type:** Optimized bush models and textures
- **Creator:** Nobiax / Yughues
- **Source:** [Bushes on OpenGameArt](https://opengameart.org/content/bushes)
- **License:** [CC0 1.0 Universal](https://creativecommons.org/publicdomain/zero/1.0/)
- **Asset details:** Selected FBX variants 01, 02, and 05 with their diffuse and
  normal textures; approximately 556, 384, and 880 triangles respectively
- **Attribution requirement:** None; creator and source are recorded for
  provenance.
- **Prototype use:** Main Street shrubs and planter vegetation
- **Downloaded location:** `assets/third_party/nature/yughues_bushes/`
- **Project wrappers:** `assets/environment/nature/yughues_bush_01.tscn`,
  `assets/environment/nature/yughues_bush_02.tscn`, and
  `assets/environment/nature/yughues_bush_05.tscn`
- **Status:** In use

### Utility Box 02

- **Asset type:** Street-side electrical utility box
- **Creator/provider:** Poly Haven
- **Source:** [Utility Box 02 on Poly Haven](https://polyhaven.com/a/utility_box_02)
- **License:** [CC0 1.0 Universal](https://polyhaven.com/license)
- **Asset details:** glTF model with 1K diffuse, ARM, and OpenGL normal textures
- **Attribution requirement:** None; provider and source are recorded for
  provenance.
- **Prototype use:** Main Street utility dressing
- **Downloaded location:** `assets/third_party/props/utilities/utility_box_02_1k.gltf/`
- **Project wrapper:** `assets/environment/props/poly_haven_utility_box_02.tscn`
- **Status:** In use

## Vehicles

### 1975 Chevrolet Impala 4-Door Sedan

- **Asset type:** Parked vehicle model
- **Creator:** Alexander Malygin
- **Source:** [1975 Chevrolet Impala on Sketchfab](https://sketchfab.com/3d-models/1975-chevrolet-impala-4-door-sedan-d969afad185a48f9b51823d8b3306d47)
- **License:** [Creative Commons Attribution 4.0 International](https://creativecommons.org/licenses/by/4.0/)
- **Asset details:** Approximately 6,211 triangles
- **Attribution requirement:** Credit the creator, asset title, source, license,
  and modifications.
- **Credit text:** "1975 Chevrolet Impala 4-door sedan" by Alexander Malygin,
  licensed under CC BY 4.0, via Sketchfab. Instantiated through a project-owned
  wrapper with adjusted scale, orientation, and simplified static collision.
- **Prototype use:** Parked Main Street vehicle
- **Downloaded location:** `assets/third_party/vehicles/1975_chevrolet_impala/`
- **Project wrapper:** `assets/environment/vehicles/parked_1975_impala.tscn`
- **Status:** In use

### Chevrolet C10 Pickup 1963

- **Asset type:** Parked pickup-truck model
- **Creator:** ROH3D
- **Source:** [Chevrolet C10 Pickup 1963 on Sketchfab](https://sketchfab.com/3d-models/low-poly-car-chevrolet-c10-pickup-1963-679354c151984747bb74310ec5af8995)
- **License:** [Creative Commons Attribution 4.0 International](https://creativecommons.org/licenses/by/4.0/)
- **Asset details:** Approximately 16,527 triangles
- **Attribution requirement:** Credit the creator, asset title, source, license,
  and modifications.
- **Credit text:** "Low-poly Chevrolet C10 Pickup 1963" by ROH3D, licensed under
  CC BY 4.0, via Sketchfab. Instantiated through a project-owned wrapper with
  adjusted scale, orientation, and simplified static collision.
- **Prototype use:** Parked Main Street pickup
- **Downloaded location:** `assets/third_party/vehicles/1963_chevrolet_c10/`
- **Project wrapper:** `assets/environment/vehicles/parked_1963_c10.tscn`
- **Status:** In use

### Shvan '92 American Panel Van

- **Asset type:** Generic American panel-van model
- **Creator:** Daniel Zhabotinsky
- **Source:** [Shvan '92 on Sketchfab](https://sketchfab.com/3d-models/shvan-92-low-poly-model-09d718c9cf72401b8534d265a06a803f)
- **License:** [Creative Commons Attribution 4.0 International](https://creativecommons.org/licenses/by/4.0/)
- **Asset details:** Approximately 23,000 triangles
- **Attribution requirement:** Credit the creator, asset title, source, license,
  and modifications.
- **Credit text:** "Shvan '92 - Low poly model" by Daniel Zhabotinsky, licensed
  under CC BY 4.0, via Sketchfab. Instantiated through a project-owned wrapper
  with adjusted scale, orientation, and simplified static collision.
- **Prototype use:** Parked Main Street commercial vehicle
- **Downloaded location:** `assets/third_party/vehicles/american_panel_van/`
- **Project wrapper:** `assets/environment/vehicles/parked_american_panel_van.tscn`
- **Status:** In use

### Rusted Alfa Romeo Old Car

- **Asset type:** Vehicle model
- **Creator:** `mimekiru`
- **Source:** [Rusted alfa romeo old car on Sketchfab](https://sketchfab.com/3d-models/rusted-alfa-romeo-old-car-83bcb308c4434646946b7ed08c154f3c)
- **License:** [Creative Commons Attribution 4.0 International](https://creativecommons.org/licenses/by/4.0/)
- **Attribution requirement:** Credit the creator, asset title, source, license,
  and any modifications.
- **Credit text:** "Rusted alfa romeo old car" by `mimekiru`, licensed under CC
  BY 4.0, via Sketchfab. Modified for use in Ashwood County 3D Prototype.
- **Prototype use:** Environment vehicle prop
- **Local location:** `assets/environment/vehicles/`
- **Status:** In use


## Old Fridge
**credits** - "Old Fridge" (https://skfb.ly/ouDRB) by sergeilihandristov is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Modern Diner
**credits** - "Modern diner" (https://skfb.ly/pDTop) by Katydid is licensed under Creative Commons Attribution-NonCommercial (http://creativecommons.org/licenses/by-nc/4.0/).

## Rusted Metal Shel
**credits** - "Rusted metal shelf" (https://skfb.ly/6U7QS) by ELIZION is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Medical crate
**credits** - "Medical Crate [derivative]" (https://skfb.ly/pJvWq) by romullus is licensed under Creative Commons Attribution-ShareAlike (http://creativecommons.org/licenses/by-sa/4.0/).

## Old dirty matteress
**credits** - "Old dirty mattress" (https://skfb.ly/6R7LL) by KIFIR is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).


## Tent
**credits** - "Tent" (https://skfb.ly/oTyQz) by trueaimisbetterthandarudas is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Home US Mailbox
**credits** - "Home US Mailbox 📫" (https://skfb.ly/oqVAr) by Glowbox 3D is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Trees and bush Pack LOWPOLY
**credits** - "Trees and bush Pack LOWPOLY" (https://skfb.ly/p6MUp) by EFX is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Old office window
**credits** - "Old Office Window" (https://skfb.ly/6WMFq) by sudreyskr is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Public bin
**credits** - "Public Bin ( Free )" (https://skfb.ly/6RnCz) by Giora is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Road Signs
**credits** - "Road Signs" (https://skfb.ly/oFIJX) by FrodoUndead is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Shop door
**credits** - "Shop_front_door" (https://skfb.ly/onyJN) by harrycrowe2001 is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## White door
**credits** - "white_door_1" (https://skfb.ly/pI8vM) by Ledeyer is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Restrauant menu sign
**credits** - "Welcome Sign Restaurant" (https://skfb.ly/oQIJv) by Yudhist.K.A is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Industrial building
**credits** - "industrial building" (https://skfb.ly/o9T9T) by spicybamer is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Old building (carpets)
**credits** - "old building" (https://skfb.ly/6TEFI) by Helindu is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Hospital door
**credits** - "HospitalDoor" (https://skfb.ly/o8n9o) by azizcharfeddine1997 is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## House door
**credits** - "House Door" (https://skfb.ly/o8xPn) by Mário Mendes is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## House door(A)
**credits** - "House_[Door A]" (https://skfb.ly/oyzFN) by Comicaroid is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Door wood
**credits** - "Door Wood" (https://skfb.ly/6zXE8) by ArtCarmesi is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Window
**credits** - "Window" (https://skfb.ly/oS7yr) by Mehdi Shahsavan is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Weathered Picnic Table
**credits** - "Weathered Picnic Table | GameReady" (https://skfb.ly/oUQ9B) by Mark Peters is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Low poly sofa
**credits** - "Low Poly  Sofa" (https://skfb.ly/pEoxI) by Ngngan is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Wooden stairs
**credits** - "Wooden Stairs_5_MB" (https://skfb.ly/p9CYU) by Mehdi Shahsavan is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Corkboard
**credits** - "Corkboard" (https://skfb.ly/pttBM) by XIN is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Window
**credits** - "Window" (https://skfb.ly/6TBZw) by JeanLescano is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## paper debris
"Paper debris" (https://skfb.ly/oDAYx) by Sousinho is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Paper
"Paper - 3MB" (https://skfb.ly/oXLrv) by Mehdi Shahsavan is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## File cabinet
"File Cabinet" (https://skfb.ly/ouwXU) by Siberia is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Antiique globe
"Antique Globe" (https://skfb.ly/ov8UO) by Matthew Collings is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## trash can
"trash can" (https://skfb.ly/o7ODJ) by Gleg3002 is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## OFfice chair
"Office Chair Game Model Download" (https://skfb.ly/6tH8U) by RanPro is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Rusty cabinet
"Rust Cabinet-Freepoly.org" (https://skfb.ly/orDIW) by Freepoly.org is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Variety of books
"Variety of Books" (https://skfb.ly/osEsy) by Spookyghostboo is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Whiteboard
"Whiteboard" (https://skfb.ly/6WHrD) by tboiston is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Post it notes
"Post it notes" (https://skfb.ly/oFrKV) by Sousinho is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Coffee mug
"Coffee Mug (School Project)" (https://skfb.ly/6toBy) by Ole Gunnar Isager is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Old metal table
"Old Metal Table (Low Poly)" (https://skfb.ly/6Runw) by Berk Gedik is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Basketball hoop panel
"Basketball Hoop Panel" (https://skfb.ly/ovYBR) by Nimrod Assaf is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Door classroom
"Door Classroom_9 MB" (https://skfb.ly/ppCRU) by Mehdi Shahsavan is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Bulletin board
"Bulletin Board" (https://skfb.ly/oQTvP) by lydia.la is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Cabinet (lockers)
"Cabinet" (https://skfb.ly/oJNSU) by Jinhong Jeong is licensed under Creative Commons Attribution-NonCommercial (http://creativecommons.org/licenses/by-nc/4.0/).

## Cobwebs asset pack
"Cobwebs Asset Pack" (https://skfb.ly/owIso) by Em Marshall is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Office drawers
"Office Drawer" (https://skfb.ly/oCPt9) by Mkky is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Basketball
"Basketball" (https://skfb.ly/Funv) by Marek Picheta is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## Picnic table
"Picnic Table" (https://skfb.ly/oBFGQ) by exiS7-Gs is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## old school cabinet (lockers)
"Old School Cabinet" (https://skfb.ly/oxY6U) by NghiaNguyeen is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

## User-Supplied Sketchfab Batch (29 July 2026)

The user supplied 42 downloaded GLBs for this environment-art slice. The
project retained 34 models whose embedded Sketchfab metadata identifies a
[CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) license. The complete
title, creator, source URL, license URL, and MD5 record is preserved in
`assets/third_party/user_supplied/ashwood_2026_07_29/SOURCES.md`; that manifest
also records why eight unsuitable, redundant, or redistribution-restricted
files were excluded. Retained models were renamed, normalized, selectively
given simple collision, and wrapped with project distance culling where useful.

The existing credit entries above cover 24 retained sources. These are the ten
retained sources that were previously missing from this central register:

| Asset | Creator | Source | Retained file | Prototype use |
| --- | --- | --- | --- | --- |
| 2001 Crown Victoria Police Interceptor Game Prop | 8sianDude | [Sketchfab](https://sketchfab.com/3d-models/2001-crown-victoria-police-interceptor-game-prop-9f30d360cee343efb5a441978ddb57bd) | `2001_crown_victoria_police_interceptor_game_prop.glb` | Police-station exterior |
| Abandoned children's slide | sergeilihandristov | [Sketchfab](https://sketchfab.com/3d-models/abandoned-childrens-slide-11de2b21ec264c0a814ac024c45ab425) | `abandoned_childrens_slide.glb` | School activity yard |
| Bike | krichwow | [Sketchfab](https://sketchfab.com/3d-models/bike-f256203558c24ea09385a861b917c75a) | `bike.glb` | School and Main Street dressing |
| Bookshelf | Idmenthal | [Sketchfab](https://sketchfab.com/3d-models/bookshelf-8df6adc634bd4004894a5e70565dc52a) | `bookshelf.glb` | School library |
| Manhole | Sirenko | [Sketchfab](https://sketchfab.com/3d-models/manhole-a657396d68fa43f0ac7da12a6403d59f) | `manhole.glb` | Main Street road dressing |
| Pencil Low | Artieee | [Sketchfab](https://sketchfab.com/3d-models/pencil-low-0f0a907cb9dd4705a84c85ca2f9db760) | `pencil_low.glb` | School classroom dressing |
| School Desk | barism09 | [Sketchfab](https://sketchfab.com/3d-models/school-desk-a74180ee97bb4917b24cd48580663b44) | `school_desk.glb` | School classrooms |
| School Cabinet (Damaged) | gozdemrl | [Sketchfab](https://sketchfab.com/3d-models/school-cabinet-damaged-81eed9f7c9194fa4ab7ceadc6171dbda) | `school_lockers_damaged.glb` | School corridors and gym |
| Some eraser two | Artieee | [Sketchfab](https://sketchfab.com/3d-models/some-eraser-two-0bdd6e5a1a0845759357b5564e588f5c) | `some_eraser_two.glb` | School classroom dressing |
| The Pen | Artieee | [Sketchfab](https://sketchfab.com/3d-models/the-pen-0501227aa9d64cca9b5df390a489ab38) | `the_pen.glb` | School and office dressing |

The Sketchfab description for `bookshelf.glb` also identifies the work as
based on Brandon Westlake's "Chocolate Beech Bookshelf (FREE)", under CC BY.
Preserve that upstream credit alongside Idmenthal's embedded attribution.

## Project-Owned Prototype Town Geometry

## Main Street CC0 Street Furniture

The following assets were downloaded and integrated on 28 July 2026. Only the
1K texture variants were retained where available.


### Street Lamp 01

- **Asset type:** Ornate cast-iron street lamp
- **Creator:** Josh Dean
- **Source:** [Street Lamp 01 on Poly Haven](https://polyhaven.com/a/street_lamp_01)
- **License:** [CC0 1.0 Universal](https://polyhaven.com/license)
- **Asset details:** Approximately 31K triangles; 3.9 metres tall; glTF with 1K
  diffuse, ARM, and OpenGL normal textures
- **Attribution requirement:** None; creator and source are recorded for
  provenance.
- **Prototype use:** Replaces the project-owned placeholder heritage lamps
  throughout Main Street
- **Downloaded location:** `assets/third_party/environment/street_furniture/poly_haven/street_lamp_01/`
- **Project wrapper:** `assets/environment/props/poly_haven_street_lamp.tscn`
- **Status:** In use

### Metal Trash Can

- **Asset type:** Weathered metal street trash can
- **Creator:** GurJas Studios
- **Source:** [Metal Trash Can on Poly Haven](https://polyhaven.com/a/metal_trash_can)
- **License:** [CC0 1.0 Universal](https://polyhaven.com/license)
- **Asset details:** Approximately 14K triangles across the supplied variants;
  glTF with 1K PBR textures. The project wrapper uses the rusted body variant
  and simplified cylinder collision.
- **Attribution requirement:** None; creator and source are recorded for
  provenance.
- **Prototype use:** Replaces the cylindrical placeholder bins on Main Street
- **Downloaded location:** `assets/third_party/props/street_furniture/poly_haven/metal_trash_can/`
- **Project wrapper:** `assets/environment/props/poly_haven_metal_trash_can.tscn`
- **Status:** In use

### Park Bench

- **Asset type:** Cast-iron park bench
- **Creator:** Teh_Bucket
- **Source:** [Park Bench on OpenGameArt](https://opengameart.org/content/park-bench)
- **License:** [CC0 1.0 Universal](https://creativecommons.org/publicdomain/zero/1.0/)
- **Asset details:** Low-poly OBJ variant; approximately 900 triangles; wrapper
  applies a project-owned neutral weathered material and box collision
- **Attribution requirement:** None; creator and source are recorded for
  provenance.
- **Prototype use:** Retained as a reusable legacy prop, but no longer used by
  the Main Street presentation after the detailed Painted Wooden Bench upgrade
- **Downloaded location:** `assets/third_party/props/street_furniture/opengameart/park_bench/`
- **Project wrapper:** `assets/environment/props/cc0_park_bench.tscn`
- **Status:** Retained, not used on Main Street

### Roadside Dressing, Service Station, Pharmacy, and Small Bush

- **Asset type:** Lightweight Godot-native environment geometry
- **Creator/provider:** Ashwood County project
- **Source:** Original project-owned meshes assembled from Godot primitives; no
  external model or texture downloads were used
- **Licence:** Project-owned; no third-party licence applies
- **Attribution requirement:** None
- **Prototype use:** House driveway, mailbox, rubbish bin, utility cabinet,
  fencing, reusable bushes, road markings, modular building shells, prototype
  interiors, service-station forecourt and pumps, and reusable hinged doors
- **Local locations:** `assets/environment/props/roadside_dressing.tscn`,
  `assets/environment/nature/small_bush.tscn`,
  `assets/environment/buildings/House/`,
  `assets/environment/buildings/Pharmacy/`,
  `assets/environment/buildings/ServiceStation/`, and
  `assets/environment/buildings/shared/`
- **Status:** In use
- **Download note:** No additional third-party files were downloaded for this
  town-dressing slice, so there are no unused downloads to remove.

## Project-Owned Procedural Atmosphere

### Wind, Insects, Crickets, and Distant Groans

- **Asset type:** Runtime-generated prototype ambience
- **Creator/provider:** Ashwood County project
- **Source:** Original procedural synthesis in the project audio controller
- **Licence:** Project-owned; no third-party licence applies
- **Attribution requirement:** None
- **Prototype use:** Naturally looping wind, daytime insects, night crickets and
  randomized distant zombie groans
- **Local location:** `scripts/world/AtmosphereAudio.cs`
- **Status:** In use
- **Download note:** No external audio files were downloaded for this slice.

## Main Street Abandonment Art Pass

### Road Damaged

- **Asset type:** Seamless damaged-asphalt PBR material
- **Creator:** Dimitrios Savva
- **Source:** [Road Damaged on Poly Haven](https://polyhaven.com/a/road_damaged)
- **License:** [CC0 1.0 Universal](https://polyhaven.com/license)
- **Asset details:** 2.2-metre surface; the project uses the existing 1K
  diffuse, packed ARM and OpenGL normal maps.
- **Attribution requirement:** None; creator and source are recorded for
  provenance.
- **Prototype use:** Main Street base asphalt beneath project-owned patch,
  crack, oil and litter overlays
- **Local location:** `assets/third_party/materials/asphalt/road_damaged_1k.gltf/`
- **Status:** In use

### Project-Owned Main Street Dressing

- **Asset type:** Surface treatment, utilities, signage, and hand-authored
  environment composition
- **Creator/provider:** Ashwood County project
- **Source:** Original Godot-native supporting geometry combined with the
  licensed asset library documented below.
- **Licence:** Project-owned
- **Prototype use:** Drainage grates, PBR asphalt repairs, cracks, oil stains,
  utility poles and wires, street signs, placement composition, distance
  culling, and bakery window-display composition. The former primitive parking
  meters, newspaper boxes, dumpsters, bike racks, weeds, transformer cylinders,
  paper rectangles, leaf discs, and concrete patch slabs are no longer present
  in the live Main Street scene.
- **Local locations:** `scenes/world/ashwood/presentation/props/`,
  `scenes/world/ashwood/presentation/storefronts/`, and
  `scenes/world/ashwood/presentation/main_street_abandonment.tscn`
- **PBR patch wrapper:**
  `assets/materials/ashwood_main_street_asphalt_patch.tres`, which reuses the
  already-credited Road Damaged diffuse, normal, and ARM maps
- **Status:** In use

### Dense Main Street Apocalypse Dressing

The following compact 1K glTF assets were downloaded from Poly Haven's
official API on 29 July 2026. All are licensed under
[CC0 1.0 Universal](https://polyhaven.com/license). Attribution is not
required, but author, source, local path, and API-provided MD5 records are
preserved for provenance.

| Asset | Author | Source | Local path | Main Street use |
| --- | --- | --- | --- | --- |
| Painted Wooden Bench | Kirill Sannikov | [Poly Haven](https://polyhaven.com/a/painted_wooden_bench) | `assets/third_party/environment/main_street_dressing/poly_haven/painted_wooden_bench/` | Weathered sidewalk and bus-stop seating |
| Fire Hydrant | Gonçalo Felício | [Poly Haven](https://polyhaven.com/a/fire_hydrant) | `assets/third_party/environment/main_street_dressing/poly_haven/fire_hydrant/` | Aged municipal hydrants |
| Hand Truck | Mutanzom3D | [Poly Haven](https://polyhaven.com/a/hand_truck) | `assets/third_party/environment/main_street_dressing/poly_haven/hand_truck/` | Abandoned deliveries and scavenging |
| Barrel 03 | Serhii Khromov | [Poly Haven](https://polyhaven.com/a/barrel_03) | `assets/third_party/environment/main_street_dressing/poly_haven/barrel_03/` | Curbside and service clutter |
| Wooden Broom | Balen | [Poly Haven](https://polyhaven.com/a/wooden_broom) | `assets/third_party/environment/main_street_dressing/poly_haven/wooden_broom/` | Fallen storefront detail |
| Cement Bag | PierreB3D | [Poly Haven](https://polyhaven.com/a/cement_bag) | `assets/third_party/environment/main_street_dressing/poly_haven/cement_bag/` | Interrupted repair-work stories |
| Industrial Storage Cart | Jule Bielitz | [Poly Haven](https://polyhaven.com/a/industrial_storage_cart) | `assets/third_party/environment/main_street_dressing/poly_haven/industrial_storage_cart/` | Looted service and delivery clusters |
| Old Tyre | James Ray Cock | [Poly Haven](https://polyhaven.com/a/old_tyre) | `assets/third_party/environment/main_street_dressing/poly_haven/old_tyre/` | Detailed replacement for the former tyre proxy |
| Planter Box 01 | James Ray Cock | [Poly Haven](https://polyhaven.com/a/planter_box_01) | `assets/third_party/environment/main_street_dressing/poly_haven/planter_box_01/` | Detailed replacement for cylinder planters |

- **Format:** 1K glTF models with 1K JPG diffuse, OpenGL-normal, and packed ARM
  textures; approximately 23.4 MB including import metadata and source records
- **Local source and checksum record:**
  `assets/third_party/environment/main_street_dressing/SOURCES.md`
- **Project composition:**
  `scenes/world/ashwood/presentation/main_street_apocalypse_dressing.tscn`
- **Project wrappers:**
  `scenes/world/ashwood/presentation/props/apocalypse_park_bench.tscn`,
  `scenes/world/ashwood/presentation/props/apocalypse_fire_hydrant.tscn`,
  `assets/environment/vehicles/old_tyre.tscn`, and
  `scenes/world/ashwood/presentation/planter.tscn`
- **Prototype use:** 190 individually composed placements across vegetation,
  refuse, service clutter, damaged street furniture, and environmental-story
  clusters. The scene also reuses previously documented licensed bushes,
  containers, barriers, bins, mailboxes, tents, furniture, and utility props.
- **Performance treatment:** Shared scenes are instanced, imported sources
  remain unmodified, simple collision is limited to substantial obstacles, and
  project-owned distance-culling ranges are applied by category.
- **Status:** In use

### Main Street Concept Sheet

- **Asset type:** Non-runtime art-direction reference
- **Creator/provider:** User supplied
- **Prototype use:** Density, abandonment, streetscape and daytime atmosphere
  direction for the Main Street art pass; not copied as scene layout or content
- **Local location:**
  `docs/design/locations/ashwood/images/mainstreet_concepts.png`
- **Status:** Design reference only

## Glen's Bakery Production Interior

The following compact asset set was downloaded from Poly Haven for the bakery
production-interior pass on 29 July 2026.

### Bakery Furniture, Equipment, Food, and Lighting

- **Assets:** [Cash Register 01](https://polyhaven.com/a/CashRegister_01),
  [Croissant](https://polyhaven.com/a/croissant),
  [Electric Stove](https://polyhaven.com/a/electric_stove),
  [Shelf 01](https://polyhaven.com/a/Shelf_01),
  [Wooden Table 02](https://polyhaven.com/a/wooden_table_02),
  [Steel Frame Shelves 01](https://polyhaven.com/a/steel_frame_shelves_01),
  and [Hanging Industrial Lamp](https://polyhaven.com/a/hanging_industrial_lamp)
- **Asset type:** Bakery retail furniture, food, back-of-house equipment,
  storage, and ceiling fixtures
- **Creator/provider:** Individual Poly Haven contributors / Poly Haven
- **License:** [CC0 1.0 Universal](https://polyhaven.com/license)
- **Asset details:** 1K glTF variants only; approximately 11 MB of source
  files across the seven assets
- **Attribution requirement:** None; provider and source pages are retained for
  provenance
- **Prototype use:** Glen's Bakery sales floor, open prep line, storage area,
  register, pastry displays, and visible period lighting
- **Downloaded location:**
  `assets/third_party/interiors/bakery/poly_haven/`
- **Local license record:**
  `assets/third_party/interiors/bakery/poly_haven/LICENSE.txt`
- **Project composition:**
  `scenes/world/ashwood/interiors/glens_bakery_interior.tscn`
- **Status:** In use

The existing licensed plastic crates and project-owned PBR wood, brick,
plaster, glass, enamel, brass, and stainless-steel materials are reused around
the downloaded focal assets. Imported sources remain unmodified.

## Greenleaf Pharmacy Production Interior

The following 1K assets were downloaded from Poly Haven on 29 July 2026 for
the Greenleaf Pharmacy production-interior pass. Every asset in the two tables
below is licensed under
[CC0 1.0 Universal](https://polyhaven.com/license); attribution is not
required, but authors and source pages are retained for provenance.

### Pharmacy Models

| Asset | Author(s) | Source | Local path | License |
| --- | --- | --- | --- | --- |
| All Purpose Cleaner | Kuutti Siitonen | [Poly Haven](https://polyhaven.com/a/all_purpose_cleaner) | `assets/third_party/interiors/pharmacy/poly_haven/all_purpose_cleaner/all_purpose_cleaner_1k.gltf` | CC0 1.0 |
| Bleach Bottle | James Ray Cock | [Poly Haven](https://polyhaven.com/a/bleach_bottle) | `assets/third_party/interiors/pharmacy/poly_haven/bleach_bottle/bleach_bottle_1k.gltf` | CC0 1.0 |
| Cardboard Box 01 | Rahul Chaudhary | [Poly Haven](https://polyhaven.com/a/cardboard_box_01) | `assets/third_party/interiors/pharmacy/poly_haven/cardboard_box_01/cardboard_box_01_1k.gltf` | CC0 1.0 |
| Chemistry Set | Jiří Ptáček | [Poly Haven](https://polyhaven.com/a/chemistry_set) | `assets/third_party/interiors/pharmacy/poly_haven/chemistry_set/chemistry_set_1k.gltf` | CC0 1.0 |
| Clipboard | ProgrammerOnCoffee | [Poly Haven](https://polyhaven.com/a/clipboard) | `assets/third_party/interiors/pharmacy/poly_haven/clipboard/clipboard_1k.gltf` | CC0 1.0 |
| Desk Lamp Arm 01 | Yann Kervran and Kuutti Siitonen | [Poly Haven](https://polyhaven.com/a/desk_lamp_arm_01) | `assets/third_party/interiors/pharmacy/poly_haven/desk_lamp_arm_01/desk_lamp_arm_01_1k.gltf` | CC0 1.0 |
| Drawer Cabinet | Ulan Cabanilla | [Poly Haven](https://polyhaven.com/a/drawer_cabinet) | `assets/third_party/interiors/pharmacy/poly_haven/drawer_cabinet/drawer_cabinet_1k.gltf` | CC0 1.0 |
| Industrial Wall Lamp | Kuutti Siitonen | [Poly Haven](https://polyhaven.com/a/industrial_wall_lamp) | `assets/third_party/interiors/pharmacy/poly_haven/industrial_wall_lamp/industrial_wall_lamp_1k.gltf` | CC0 1.0 |
| Long Life Food | Mia Pecina Zorko | [Poly Haven](https://polyhaven.com/a/long_life_food) | `assets/third_party/interiors/pharmacy/poly_haven/long_life_food/long_life_food_1k.gltf` | CC0 1.0 |
| Medical Box | Ulan Cabanilla | [Poly Haven](https://polyhaven.com/a/medical_box) | `assets/third_party/interiors/pharmacy/poly_haven/medical_box/medical_box_1k.gltf` | CC0 1.0 |
| Medical Tape | Miroslav Turura | [Poly Haven](https://polyhaven.com/a/medical_tape) | `assets/third_party/interiors/pharmacy/poly_haven/medical_tape/medical_tape_1k.gltf` | CC0 1.0 |
| Metal Office Desk | Ulan Cabanilla | [Poly Haven](https://polyhaven.com/a/metal_office_desk) | `assets/third_party/interiors/pharmacy/poly_haven/metal_office_desk/metal_office_desk_1k.gltf` | CC0 1.0 |
| Mounted Fluorescent Lights | Ulan Cabanilla | [Poly Haven](https://polyhaven.com/a/mounted_fluorescent_lights) | `assets/third_party/interiors/pharmacy/poly_haven/mounted_fluorescent_lights/mounted_fluorescent_lights_1k.gltf` | CC0 1.0 |
| Office Notepads | Ulan Cabanilla | [Poly Haven](https://polyhaven.com/a/office_notepads) | `assets/third_party/interiors/pharmacy/poly_haven/office_notepads/office_notepads_1k.gltf` | CC0 1.0 |
| School Chair 01 | Ethan Place | [Poly Haven](https://polyhaven.com/a/SchoolChair_01) | `assets/third_party/interiors/pharmacy/poly_haven/SchoolChair_01/SchoolChair_01_1k.gltf` | CC0 1.0 |
| Stationery Supplies | Mateusz Sadek | [Poly Haven](https://polyhaven.com/a/stationery_supplies) | `assets/third_party/interiors/pharmacy/poly_haven/stationery_supplies/stationery_supplies_1k.gltf` | CC0 1.0 |
| Steel Frame Shelves 02 | James Ray Cock | [Poly Haven](https://polyhaven.com/a/steel_frame_shelves_02) | `assets/third_party/interiors/pharmacy/poly_haven/steel_frame_shelves_02/steel_frame_shelves_02_1k.gltf` | CC0 1.0 |
| Trashbag | Benny Weimer | [Poly Haven](https://polyhaven.com/a/trashbag) | `assets/third_party/interiors/pharmacy/poly_haven/trashbag/trashbag_1k.gltf` | CC0 1.0 |
| Vintage Crutches 01 | James Ray Cock | [Poly Haven](https://polyhaven.com/a/vintage_crutches_01) | `assets/third_party/interiors/pharmacy/poly_haven/vintage_crutches_01/vintage_crutches_01_1k.gltf` | CC0 1.0 |
| Vintage Wooden Drawer 01 | James Ray Cock | [Poly Haven](https://polyhaven.com/a/vintage_wooden_drawer_01) | `assets/third_party/interiors/pharmacy/poly_haven/vintage_wooden_drawer_01/vintage_wooden_drawer_01_1k.gltf` | CC0 1.0 |
| Wall Clock | PierreB3D | [Poly Haven](https://polyhaven.com/a/wall_clock) | `assets/third_party/interiors/pharmacy/poly_haven/wall_clock/wall_clock_1k.gltf` | CC0 1.0 |
| Wheelchair 01 | Garreth Dean | [Poly Haven](https://polyhaven.com/a/wheelchair_01) | `assets/third_party/interiors/pharmacy/poly_haven/wheelchair_01/wheelchair_01_1k.gltf` | CC0 1.0 |
| Wooden Display Shelves 01 | James Ray Cock | [Poly Haven](https://polyhaven.com/a/wooden_display_shelves_01) | `assets/third_party/interiors/pharmacy/poly_haven/wooden_display_shelves_01/wooden_display_shelves_01_1k.gltf` | CC0 1.0 |

### Pharmacy PBR Surfaces

| Asset | Author(s) | Source | Downloaded location | Project material | License |
| --- | --- | --- | --- | --- | --- |
| Beige Wall 001 | Dimitrios Savva and Rico Cilliers | [Poly Haven](https://polyhaven.com/a/beige_wall_001) | `assets/third_party/interiors/pharmacy/materials/poly_haven/beige_wall_001/` | `assets/materials/greenleaf_pharmacy_wall.tres` | CC0 1.0 |
| Ceiling Interior | Dimitrios Savva | [Poly Haven](https://polyhaven.com/a/ceiling_interior) | `assets/third_party/interiors/pharmacy/materials/poly_haven/ceiling_interior/` | `assets/materials/greenleaf_pharmacy_ceiling.tres` | CC0 1.0 |
| Green Metal Rust | Rob Tuytel | [Poly Haven](https://polyhaven.com/a/green_metal_rust) | `assets/third_party/interiors/pharmacy/materials/poly_haven/green_metal_rust/` | `assets/materials/greenleaf_pharmacy_green_metal.tres` and `assets/materials/greenleaf_pharmacy_sign.tres` | CC0 1.0 |
| Interior Tiles | Charlotte Baglioni | [Poly Haven](https://polyhaven.com/a/interior_tiles) | `assets/third_party/interiors/pharmacy/materials/poly_haven/interior_tiles/` | `assets/materials/greenleaf_pharmacy_bathroom_tile.tres` | CC0 1.0 |
| Old Linoleum Flooring 01 | Charlotte Baglioni | [Poly Haven](https://polyhaven.com/a/old_linoleum_flooring_01) | `assets/third_party/interiors/pharmacy/materials/poly_haven/old_linoleum_flooring_01/` | `assets/materials/greenleaf_pharmacy_linoleum.tres` | CC0 1.0 |

### Toilets Sanitary Fixtures

- **Asset:** [Toilets](https://opengameart.org/content/toilets)
- **Creator:** `loafbrr_1`
- **Provider:** OpenGameArt
- **License:** [CC0 1.0 Universal](https://creativecommons.org/publicdomain/zero/1.0/)
- **Attribution requirement:** None; creator and source are recorded for
  provenance.
- **Retained subset:** One UV-mapped sink, one UV-mapped round toilet, their
  required authored 1K diffuse, normal and packed AoRM maps, and the supplied
  license readme. Unused pack contents were not retained.
- **Downloaded location:**
  `assets/third_party/interiors/shared/open_game_art/loafbrr_toilets/`
- **Project scene wrappers:**
  `assets/third_party/interiors/shared/open_game_art/loafbrr_toilets/objects/sink_a.tscn`
  and
  `assets/third_party/interiors/shared/open_game_art/loafbrr_toilets/objects/toilet_round_a.tscn`
- **Project material wrappers:**
  `assets/third_party/interiors/shared/open_game_art/loafbrr_toilets/materials/sink_a.tres`,
  `assets/third_party/interiors/shared/open_game_art/loafbrr_toilets/materials/toilet_flush_box.tres`,
  `assets/third_party/interiors/shared/open_game_art/loafbrr_toilets/materials/toilet_round.tres`,
  and
  `assets/third_party/interiors/shared/open_game_art/loafbrr_toilets/materials/toilet_round_seat.tres`
- **Local license record:**
  `assets/third_party/interiors/shared/open_game_art/loafbrr_toilets/README.txt`
- **Status:** In use

Greenleaf Pharmacy also reuses the already documented Poly Haven Cash Register
01, Steel Frame Shelves 01, and Wooden Table 02 from the Glen's Bakery pack.
No new door model was downloaded: the existing CC BY 4.0 Shop Front Door is
instanced through
`assets/environment/buildings/Pharmacy/front_door.tscn`; its full credit
remains in the earlier door-assets entry.

## Additional Main Street Business Interior Assets

The following compact 1K glTF models were downloaded through Poly Haven's
official API on 29 July 2026. All are licensed under
[CC0 1.0 Universal](https://polyhaven.com/license); attribution is not
required, but the available author and source information is retained for
provenance.

### Ashwood Grocery Models

| Asset | Author/provider | Source | Local path | License |
| --- | --- | --- | --- | --- |
| Bananas | Poly Haven contributors | [Poly Haven](https://polyhaven.com/a/bananas) | `assets/third_party/interiors/ashwood_grocery/poly_haven/bananas/` | CC0 1.0 |
| Food Apple 01 | Poly Haven contributors | [Poly Haven](https://polyhaven.com/a/food_apple_01) | `assets/third_party/interiors/ashwood_grocery/poly_haven/food_apple_01/` | CC0 1.0 |
| Yellow Onion | Poly Haven contributors | [Poly Haven](https://polyhaven.com/a/yellow_onion) | `assets/third_party/interiors/ashwood_grocery/poly_haven/yellow_onion/` | CC0 1.0 |
| Sweet Potato | Poly Haven contributors | [Poly Haven](https://polyhaven.com/a/sweet_potato) | `assets/third_party/interiors/ashwood_grocery/poly_haven/sweet_potato/` | CC0 1.0 |
| Wicker Basket 01 | Poly Haven contributors | [Poly Haven](https://polyhaven.com/a/wicker_basket_01) | `assets/third_party/interiors/ashwood_grocery/poly_haven/wicker_basket_01/` | CC0 1.0 |
| Russian Food Cans 01 | Poly Haven contributors | [Poly Haven](https://polyhaven.com/a/russian_food_cans_01) | `assets/third_party/interiors/ashwood_grocery/poly_haven/russian_food_cans_01/` | CC0 1.0 |
| Wine Bottles 01 | Poly Haven contributors | [Poly Haven](https://polyhaven.com/a/wine_bottles_01) | `assets/third_party/interiors/ashwood_grocery/poly_haven/wine_bottles_01/` | CC0 1.0 |

- **Prototype use:** Produce displays, stocked grocery aisles, baskets, cans,
  and bottle shelving
- **Project composition:** `assets/environment/buildings/AshwoodGrocery/`
- **Local source record:**
  `assets/third_party/interiors/ashwood_grocery/SOURCES.md`
- **Status:** In use

### Miller Hardware Models

| Asset | Author | Source | Local path | License |
| --- | --- | --- | --- | --- |
| Drill 01 | Fernando Quinn | [Poly Haven](https://polyhaven.com/a/Drill_01) | `assets/third_party/interiors/miller_hardware/poly_haven/Drill_01/` | CC0 1.0 |
| Adjustable Wrench | Mateusz Sadek | [Poly Haven](https://polyhaven.com/a/adjustable_wrench) | `assets/third_party/interiors/miller_hardware/poly_haven/adjustable_wrench/` | CC0 1.0 |
| Crowbar 01 | Alexander Otterbeck | [Poly Haven](https://polyhaven.com/a/crowbar_01) | `assets/third_party/interiors/miller_hardware/poly_haven/crowbar_01/` | CC0 1.0 |
| Rusted Hacksaw | Dabou Master | [Poly Haven](https://polyhaven.com/a/rusted_hacksaw) | `assets/third_party/interiors/miller_hardware/poly_haven/rusted_hacksaw/` | CC0 1.0 |
| Screwdrivers 02 | BKS | [Poly Haven](https://polyhaven.com/a/screwdrivers_02) | `assets/third_party/interiors/miller_hardware/poly_haven/screwdrivers_02/` | CC0 1.0 |
| Metal Toolbox | Mateusz Sadek | [Poly Haven](https://polyhaven.com/a/metal_toolbox) | `assets/third_party/interiors/miller_hardware/poly_haven/metal_toolbox/` | CC0 1.0 |
| Metal Jerrycan | Sean Buckley | [Poly Haven](https://polyhaven.com/a/metal_jerrycan) | `assets/third_party/interiors/miller_hardware/poly_haven/metal_jerrycan/` | CC0 1.0 |
| Ladder Sectioned 01 | MP | [Poly Haven](https://polyhaven.com/a/ladder_sectioned_01) | `assets/third_party/interiors/miller_hardware/poly_haven/ladder_sectioned_01/` | CC0 1.0 |

- **Prototype use:** Tool displays, counter stock, utility supplies, warehouse
  storage, and loading-area dressing
- **Project composition:** `assets/environment/buildings/MillerHardware/`
- **Local source record:**
  `assets/third_party/interiors/miller_hardware/SOURCES.md`
- **Status:** In use

### Ashwood Police Station Models

| Asset | Author/provider | Source | Local path | License |
| --- | --- | --- | --- | --- |
| Ammo Box | Poly Haven contributors | [Poly Haven](https://polyhaven.com/a/ammo_box) | `assets/third_party/interiors/ashwood_police_station/poly_haven/ammo_box/` | CC0 1.0 |
| Signal Flashlight | Poly Haven contributors | [Poly Haven](https://polyhaven.com/a/signal_flashlight) | `assets/third_party/interiors/ashwood_police_station/poly_haven/signal_flashlight/` | CC0 1.0 |
| Vintage Radio Transceiver | Poly Haven contributors | [Poly Haven](https://polyhaven.com/a/vintage_radio_transceiver) | `assets/third_party/interiors/ashwood_police_station/poly_haven/vintage_radio_transceiver/` | CC0 1.0 |
| Megaphone 01 | Poly Haven contributors | [Poly Haven](https://polyhaven.com/a/Megaphone_01) | `assets/third_party/interiors/ashwood_police_station/poly_haven/Megaphone_01/` | CC0 1.0 |
| Binder Notebook | Poly Haven contributors | [Poly Haven](https://polyhaven.com/a/binder_notebook) | `assets/third_party/interiors/ashwood_police_station/poly_haven/binder_notebook/` | CC0 1.0 |
| Old Gas Mask | Poly Haven contributors | [Poly Haven](https://polyhaven.com/a/old_gas_mask) | `assets/third_party/interiors/ashwood_police_station/poly_haven/old_gas_mask/` | CC0 1.0 |
| Security Camera 01 | Poly Haven contributors | [Poly Haven](https://polyhaven.com/a/security_camera_01) | `assets/third_party/interiors/ashwood_police_station/poly_haven/security_camera_01/` | CC0 1.0 |

- **Prototype use:** Reception and office clutter, evidence and armory
  dressing, security equipment, and basement cell-block details
- **Project composition:**
  `assets/environment/buildings/AshwoodPoliceStation/`
- **Local source record:**
  `assets/third_party/interiors/ashwood_police_station/SOURCES.md`
- **Status:** In use

### Reused Shared Interior Assets and PBR Surfaces

Ashwood Grocery, Miller Hardware, and Ashwood Police Station also instance
already-present Bakery, Pharmacy, Diner, Willow Outfitters, common-crate, and
shared sanitary assets without modifying their source files. This includes the
previously credited CC0 Poly Haven furniture, shelving, registers, fluorescent
fixtures and PBR surfaces; the CC0 OpenGameArt toilet-and-sink subset; and the
existing CC BY door and mattress models where used. Their original records
remain in this document and in the source manifests beside those shared packs,
so they are not duplicated asset by asset here.

## August 2026 Survival Systems and Vegetation Pass

### Tree Small 02

- **Asset type:** High-detail tree model and 1K PBR texture set
- **Creator:** Rico Cilliers
- **Provider/source:** [Tree Small 02 on Poly Haven](https://polyhaven.com/a/tree_small_02)
- **License:** [CC0 1.0 Universal](https://polyhaven.com/license)
- **Commercial use:** Permitted; attribution is not required
- **Downloaded:** 2 August 2026 through Poly Haven's official public API; all
  retained downloads matched the API-provided MD5 values
- **Retained source:**
  `assets/third_party/environment/vegetation/poly_haven/tree_small_02/`
- **Editor/source-control note:** A local `.gdignore` keeps the unused
  2,062,487-triangle authoring source out of Godot's import scan. The retained
  source is approximately 101 MB and should use Git LFS or a reproducible
  acquisition step before a public repository release.
- **Project derivative:**
  `assets/environment/nature/ashwood_hero_tree_small_02.glb`
- **Reproducible optimization:**
  `tools/blender/optimize_poly_haven_tree_small_02.py`
- **Modification record:** Material parts were separated, redundant planar
  leaf tessellation was dissolved, part-specific decimation was applied, and
  the mesh was rejoined/exported without modifying the retained source. The
  measured project input was reduced from 2,062,487 to 120,429 triangles.
- **Derivative SHA-256:**
  `AB221DAB7AC84262D37EE857F325D110E2D7B2588158AAF317B4E7F486182B00`
- **Prototype use:** Four seasonally tinted, range-limited foreground and
  midground anchors on Main Street; lightweight existing trees remain in the
  distant canopy
- **Local provenance record:**
  `assets/third_party/environment/vegetation/poly_haven/tree_small_02/SOURCES.md`
- **Status:** In use

### Kenney RPG Audio

- **Asset type:** RPG game-foley audio pack (51 extracted OGG files)
- **Creator/provider:** Kenney Vleugels / Kenney
- **Source:** [RPG Audio on Kenney](https://kenney.nl/assets/rpg-audio)
- **License:** Creative Commons Zero (CC0 1.0 Universal)
- **Commercial use:** Permitted; attribution is not required. Kenney is
  credited here for provenance.
- **Downloaded:** 2 August 2026 from Kenney's official asset download
- **Local location:** `assets/third_party/audio/kenney_rpg_audio/`
- **Local license:**
  `assets/third_party/audio/kenney_rpg_audio/License.txt`
- **Downloaded archive SHA-256 (acquisition record):**
  `6DBEAF8544DA958D8F2ADCB4A4A4B76C1ADE34A05F8AB9EDCCD327DA7375F38B`
- **Repository note:** The extracted OGG files and local `License.txt` are the
  retained project sources. Download archives are excluded by the repository's
  `*.zip` rule, so the hash records the verified acquisition rather than a file
  expected to survive a normal commit.
- **Files currently used:** `footstep00.ogg` through `footstep09.ogg` for
  distance-driven player footsteps; `doorOpen_1.ogg` and `doorClose_2.ogg` for
  world doors; `cloth2.ogg`, `cloth4.ogg`, and `chop.ogg` for melee motion and
  contact; `handleSmallLeather.ogg`, `handleSmallLeather2.ogg`,
  `handleCoins2.ogg`, `metalClick.ogg`, and `bookPlace2.ogg` for inventory and
  storage feedback
- **Status:** In use

## Release Checklist

- [x] Record source and license links for all listed assets.
- [x] Record creators and ready-to-use credits for all CC BY assets.
- [ ] Retain local copies of applicable licenses or download records.
- [ ] Include all required CC BY credits in any distributed build.
- [ ] Note material modifications in the final credits.
