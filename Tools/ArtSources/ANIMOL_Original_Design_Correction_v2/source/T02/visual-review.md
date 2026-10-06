# T02 finishing review

- Edit source: the original T02 atlas, not the previous Batch01 revision.
- Four-panel composition and original whale, windmill, floating islands, coral bridge, rope fence and foreground whale banner remain identifiable in their original locations.
- The bridge now uses broad coral faces, connected burgundy post/arch shadows and purple underside planes. Scattered cap, deck and post flecks were removed in the focused second image edit.
- The metal plates retain two purposeful rivets; these are hardware details, not random surface shading.
- Native far/mid/near alpha masks are identical to the original. The platform preserves the prior upper sky-bar removal and clips edited-bridge alpha to the original silhouette below y448. This removes 428 fringe pixels; exact original alpha identity is therefore not claimed for that layer. The floor at y526 remains fully opaque across x70–281.
- All runtime layers are 352 x 704, binary alpha and Sweetie16-only with nearest mapping and no dithering.
- Native 4x bridge crop was inspected after export. The arch opening count remains one; surface speckling on the bridge posts is substantially reduced. Pixel outline/connected shadow steps remain visible as deliberate small-scale geometry.
- The partially clipped foreground post on the extreme left remains the original composition. Its brace highlight is more irregular than the new main bridge's metal highlights; this is a remaining polish limitation, not an assertion that every jaggy in all decorative pieces is eliminated.

All artistic changes came from the built-in image-generation edit. The export script only registers, samples, maps the fixed palette, handles alpha and assembles the output.
