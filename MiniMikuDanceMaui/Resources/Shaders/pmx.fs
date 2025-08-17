$input v_normal, v_texcoord0
#include <bgfx_shader.sh>

uniform vec4 u_lightDir;
uniform vec4 u_lightColor;
uniform vec4 u_shadeParam;
uniform vec4 u_color;
SAMPLER2D(s_texColor, 0);

void main()
{
    vec3 n = normalize(v_normal);
    vec3 l = normalize(u_lightDir.xyz);
    float diff = max(dot(n, l) + u_shadeParam.x, 0.0);
    float toon = max(u_shadeParam.y, 1.0);
    diff = floor(diff * toon) / toon;
    float rim = pow(1.0 - clamp(n.z, 0.0, 1.0), 5.0) * u_shadeParam.z;
    float amb = u_shadeParam.w;
    vec4 tex = texture2D(s_texColor, v_texcoord0) * u_color;
    vec3 light = u_lightColor.rgb * diff + vec3(rim + amb);
    gl_FragColor = vec4(tex.rgb * light, tex.a);
}
