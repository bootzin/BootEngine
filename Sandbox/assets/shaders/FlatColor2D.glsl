#type fragment
#version 430 core

layout(set = 0, binding = 2) uniform Color
{
	vec4 fsin_Color;
};

layout(location = 0) out vec4 fsout_Color;

void main()
{
	fsout_Color = fsin_Color;
}

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

layout(location = 0) in vec3 Position;

void main()
{
	gl_Position = view_projection_matrix * model_matrix * vec4(Position.x, Position.y, Position.y, 1);
}