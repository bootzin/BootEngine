#type vertex
#version 430 core

layout(set = 0, binding = 0) uniform ViewProjection
{
	mat4 view_projection_matrix;
};

layout(set = 0, binding = 1) uniform Transform
{
	mat4 model_matrix;
};

layout(location = 0) in vec2 Position;
layout(location = 1) in vec2 TexCoord;

layout(location = 0) out vec2 outTexCoord;

void main()
{
	outTexCoord = TexCoord;
	gl_Position = view_projection_matrix * model_matrix * vec4(Position, 0, 1);
}

#type fragment
#version 430 core

layout(location = 0) in vec2 TexCoord;
layout(location = 0) out vec4 color;

layout(set = 0, binding = 2) uniform texture2D Texture;
layout(set = 0, binding = 3) uniform sampler Sampler;

void main()
{
	color = texture(sampler2D(Texture, Sampler), TexCoord);
}