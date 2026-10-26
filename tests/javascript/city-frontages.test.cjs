const {test}=require('node:test');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const WorldSchematic=require('../../src/WorldCreator/WorldCreator.Core/Schematic/world-schematic.js');
for(const name of ['four-frontages','mixed-frontages']) {
    test(`${name}: reserve/rotation metadata and every engine geometry coordinate survive rendering`,()=>{
        const root=`generated/procedural-city/enhanced/${name}/`;
        const world=JSON.parse(fs.readFileSync(root+'world-metadata.json'));
        const city=JSON.parse(fs.readFileSync(root+'city.json'));
        const plan=WorldSchematic.layout(world), byId=new Map(plan.boxes.map(b=>[b.id,b]));
        assert.equal(plan.coordinateSpace,'engine-world-meters');
        assert.equal(plan.boxes.length,world.Nodes.length);
        for(const source of world.MapGeometry.Boxes) {
            const b=byId.get(source.NodeId);
            assert.deepEqual([b.x,b.y,b.width,b.height],[source.X,source.Y,source.Width,source.Height]);
        }
        assert.deepEqual(new Set(city.Parcels.map(p=>p.Frontage)),new Set(['North','East','South','West']));
        assert.ok(world.Nodes.some(n=>n.Kind==='courtyard'));
        assert.ok(world.Nodes.some(n=>n.Kind==='passage'));
        assert.ok(world.Connections.some(c=>c.Kind==='interior-access'));
        for(const s of city.Structures) {
            const node=world.Nodes.find(n=>n.Id===`${city.Id}/structure/${s.Id}`);
            assert.equal(node.Properties.rotation_degrees,s.RotationDegrees);
            assert.deepEqual(node.Position,[s.Bounds.X+s.Bounds.Width/2,s.Bounds.Y+s.Bounds.Height/2]);
        }
        assert.equal(fs.readFileSync(root+'schematic.js','utf8'),fs.readFileSync('src/WorldCreator/WorldCreator.Core/Schematic/world-schematic.js','utf8'));
        const manifest=JSON.parse(fs.readFileSync(root+'package-manifest.json'));
        assert.equal(Object.keys(manifest.Files).length,8);
        assert.ok(manifest.Files['schematic.js']);
    });
}
