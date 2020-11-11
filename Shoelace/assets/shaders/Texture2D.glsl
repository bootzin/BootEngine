#type vertex
#version 430 core

layout(set = 0, binding = 0) uniform ViewProjection
{
	mat4 view_projection_matrix;
};

layout(location = 0) in vec3 Position;
layout(location = 1) in vec2 TexCoord;
layout(location = 2) in vec3 InstancePosition;
layout(location = 3) in vec2 InstanceScale;
layout(location = 4) in float InstanceRotation;
layout(location = 5) in vec4 InstanceColor;

layout(location = 0) out vec2 fsTexCoord;
layout(location = 1) out vec4 fsColor;

void main()
{
	float sinZ = sin(InstanceRotation);
	float cosZ = cos(InstanceRotation);

	mat3 rotationZMatrix = mat3(cosZ, -sinZ, 0, sinZ, cosZ, 0, 0, 0, 1);
	mat3 scaleMatrix = mat3(InstanceScale.x, 0, 0, 0, InstanceScale.y, 0, 0, 0, 1);
	vec3 transformedPosition = (scaleMatrix * rotationZMatrix * Position) + InstancePosition;

	fsTexCoord = TexCoord;
	fsColor = InstanceColor;
	gl_Position = view_projection_matrix * vec4(transformedPosition, 1.0);
}

#type fragment
#version 430 core

layout(location = 0) in vec2 fsTexCoord;
layout(location = 1) in vec4 fsColor;
layout(location = 0) out vec4 outColor;

layout(set = 0, binding = 1) uniform texture2D Texture;
layout(set = 0, binding = 2) uniform sampler Sampler;

void main()
{
	outColor = texture(sampler2D(Texture, Sampler), fsTexCoord) * fsColor;
}