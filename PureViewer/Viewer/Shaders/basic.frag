#version 330 core
in vec3 FragPos;
in vec3 Normal;

uniform vec3 lightDir;
uniform vec3 lightColor;
uniform vec3 ambient;
uniform vec3 objectColor;

out vec4 FragColor;

void main()
{
    vec3 norm = normalize(Normal);
    float diff = max(dot(norm, -lightDir), 0.0);
    vec3 color = ambient * objectColor + diff * lightColor * objectColor;
    FragColor = vec4(color, 1.0);
}
