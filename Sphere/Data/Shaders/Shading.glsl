-- Vertex
#version 400

in vec3 Position;

uniform mat4 ModelViewProjectionMatrix;

void main()
{
	gl_Position = ModelViewProjectionMatrix * vec4(Position, 1.0);
}

-- Fragment.Light.Directional
#version 400

out vec4 FragColor;

uniform sampler2D GPosition;
uniform sampler2D GDiffuse;
uniform sampler2D GNormal;
uniform sampler2D GTexCoord;

uniform vec2 InverseScreenSize;

void main()
{
	vec2 coord = gl_FragCoord.xy * InverseScreenSize;
	vec3 pos = texture(GPosition, coord).xyz;
	vec3 color = texture(GDiffuse, coord).xyz;
	vec3 normal = normalize(texture(GNormal, coord).xyz);

	vec3 lightDirection = vec3(0,1,0);

	FragColor = vec4(color * dot(normal,lightDirection), 1.0);
}