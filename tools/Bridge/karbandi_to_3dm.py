import json, rhino3dm as r3
D=json.load(open('/tmp/km/km.json'))
COL={'Ribs':(176,98,62),'Wall arches':(150,82,52),'Shamseh':(214,170,128),'Shells':(236,228,212),'Sousani':(226,214,192)}
for key,title in [('hmq','karbandi-haj-mohammad-qoli'),('pirnia','karbandi-pirnia-4x3'),('square','karbandi-square-16-4')]:
    f=r3.File3dm(); f.Settings.ModelUnitSystem=r3.UnitSystem.Millimeters
    li={}
    for name,c in COL.items():
        L=r3.Layer(); L.Name=name; L.Color=(c[0],c[1],c[2],255); li[name]=f.Layers.Add(L)
    Lb=r3.Layer(); Lb.Name='Base'; Lb.Color=(90,90,90,255); lb=f.Layers.Add(Lb)
    for m in D[key]['meshes']:
        M=r3.Mesh()
        for v in m['v']: M.Vertices.Add(v[0],v[1],v[2])
        for fc in m['f']:
            if len(fc)==3: M.Faces.AddFace(fc[0],fc[1],fc[2])
            else: M.Faces.AddFace(fc[0],fc[1],fc[2],fc[3])
        M.Normals.ComputeNormals(); M.Compact()
        a=r3.ObjectAttributes(); a.LayerIndex=li[m['layer']]; a.Name=m['name']
        f.Objects.AddMesh(M,a)
    pts=[r3.Point3d(*p) for p in D[key]['base']]; pts.append(pts[0])
    a=r3.ObjectAttributes(); a.LayerIndex=lb
    f.Objects.AddPolyline(r3.Point3dList(pts) if hasattr(r3,'Point3dList') else pts,a)
    out=f'/home/claude/hezarbaaf/examples/{title}.3dm'; f.Write(out,7); print(out)
