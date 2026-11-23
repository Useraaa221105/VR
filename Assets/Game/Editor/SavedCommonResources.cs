using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Podz.EditorTools
{
    /// <summary>Content-addressed shared baked materials/textures. Never overwrite an existing edited asset.</summary>
    internal static class SavedCommonResources
    {
        const string Root="Assets/Game/Dungeon/Common/GeneratedResources";
        static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        static string Hash(string text)=>Hash(Encoding.UTF8.GetBytes(text));
        static string N(float value)=>value.ToString("R",CultureInfo.InvariantCulture);
        static string V(Vector4 v)=>N(v.x)+","+N(v.y)+","+N(v.z)+","+N(v.w);
        static string TextureKey(Texture value)
        {
            if(value==null)return "none";
            string path=AssetDatabase.GetAssetPath(value);
            if(!string.IsNullOrEmpty(path))return "asset:"+AssetDatabase.AssetPathToGUID(path)+":"+path;
            if(!(value is Texture2D texture) || !texture.isReadable)throw new InvalidOperationException("Нельзя сохранить временную нечитаемую текстуру: "+value.name);
            return "generated:"+texture.width+":"+texture.height+":"+texture.format+":"+texture.mipmapCount+":"+texture.wrapModeU+":"+texture.wrapModeV+":"+texture.wrapModeW+":"+texture.filterMode+":"+texture.anisoLevel+":"+N(texture.mipMapBias)+":"+texture.isDataSRGB+":"+Hash(texture.GetRawTextureData());
        }
        public static Texture SaveTexture(Texture source)
        {
            if(EditorUtility.IsPersistent(source))return source;
            string key=TextureKey(source),path=Root+"/Texture_"+Hash(key)+".asset";
            DungeonPrefabCatalogEditor.Folder(Root);
            var existing=AssetDatabase.LoadAssetAtPath<Texture>(path);
            if(existing!=null)
            {
                // Existing textures are persistent: compare actual pixels/settings rather than their GUID.
                var clone=Object.Instantiate(existing);
                try {if(TextureKey(clone)==key)return existing;}finally{Object.DestroyImmediate(clone);}
                path=AssetDatabase.GenerateUniqueAssetPath(path);
            }
            var saved=Object.Instantiate(source);saved.name=source.name;AssetDatabase.CreateAsset(saved,path);return saved;
        }
        static string MaterialKey(Material material)
        {
            var text=new StringBuilder(material.shader.name+"|"+material.renderQueue+"|"+material.enableInstancing+"|"+material.doubleSidedGI+"|"+material.globalIlluminationFlags);
            var keywords=(string[])material.shaderKeywords.Clone();Array.Sort(keywords,StringComparer.Ordinal);text.Append("|"+string.Join(",",keywords));
            for(int i=0;i<ShaderUtil.GetPropertyCount(material.shader);i++)
            {
                string name=ShaderUtil.GetPropertyName(material.shader,i);var type=ShaderUtil.GetPropertyType(material.shader,i);text.Append("|"+name+":"+type+":");
                if(type==ShaderUtil.ShaderPropertyType.TexEnv) {text.Append(TextureKey(material.GetTexture(name)));var a=material.GetTextureScale(name);var b=material.GetTextureOffset(name);text.Append(V(new Vector4(a.x,a.y,b.x,b.y)));}
                else if(type==ShaderUtil.ShaderPropertyType.Color) {var c=material.GetColor(name);text.Append(V(new Vector4(c.r,c.g,c.b,c.a)));}
                else if(type==ShaderUtil.ShaderPropertyType.Vector)text.Append(V(material.GetVector(name)));
                else if(type.ToString()=="Int")text.Append(material.GetInt(name));
                else text.Append(N(material.GetFloat(name)));
            }
            return text.ToString();
        }
        public static Material SaveMaterial(Material candidate)
        {
            DungeonPrefabCatalogEditor.Folder(Root);string key=MaterialKey(candidate),path=Root+"/Material_"+Hash(key)+".mat";
            var existing=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(existing!=null && MaterialKey(existing)==key){Object.DestroyImmediate(candidate);return existing;}
            if(existing!=null)path=AssetDatabase.GenerateUniqueAssetPath(path);
            AssetDatabase.CreateAsset(candidate,path);return candidate;
        }
    }
}
