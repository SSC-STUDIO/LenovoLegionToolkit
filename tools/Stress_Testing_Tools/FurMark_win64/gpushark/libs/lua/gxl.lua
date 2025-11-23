
--[[
-- Helper lib for GeeXLab demos.


Version 0.1.1
- fixed a bug in set_sampler_params(): addressing mode for 
  clamp2edge, clamp2border and mirror was not applied.

--]]


gxl = {
  _version_major = 0,
  _version_minor = 1,
  _version_patch = 1

}


----------------------------------------------------------------------------
function gxl.load_texture_rgba_u8(filename)
  local PF_U8_RGB = 1
  local PF_U8_RGBA = 3
  local pixel_format = PF_U8_RGBA
  local gen_mipmaps = 1
  local compressed_texture = 0
  local free_cpu_memory = 1
  --local demo_dir = gh_utils.get_demo_dir()  
  --local t = gh_texture.create_from_file_v6(demo_dir .. filename, pixel_format, gen_mipmaps, compressed_texture)
  local t = gh_texture.create_from_file_v6(filename, pixel_format, gen_mipmaps, compressed_texture)
  return t
end  

function gxl.load_texture_rgba_u8_zip(zip_filename, filename)
  local PF_U8_RGB = 1
  local PF_U8_RGBA = 3
  local pixel_format = PF_U8_RGBA
  local upload_to_gpu = 1
  local mipmap = 1
  local compressed_format = "" 
  local t = gh_texture.create_from_zip_file(zip_filename, filename, upload_to_gpu, PF_U8_RGBA, 0, mipmap, compressed_format)
  return t
end  


----------------------------------------------------------------------------
function gxl.load_texture_rgba_f32(filename)
  local PF_F32_RGBA = 6
  local pixel_format = PF_F32_RGBA
  local gen_mipmaps = 1
  local compressed_texture = 0
  local free_cpu_memory = 1
  --local demo_dir = gh_utils.get_demo_dir()  
  --local t = gh_texture.create_from_file_v6(demo_dir .. filename, pixel_format, gen_mipmaps, compressed_texture)
  local t = gh_texture.create_from_file_v6(filename, pixel_format, gen_mipmaps, compressed_texture)
  return t
end  

function gxl.load_texture_rgba_f32_zip(zip_filename, filename)
  local PF_F32_RGBA = 6
  local pixel_format = PF_F32_RGBA
  local upload_to_gpu = 1
  local mipmap = 1
  local compressed_format = "" 
  local t = gh_texture.create_from_zip_file(zip_filename, filename, upload_to_gpu, PF_U8_RGBA, 0, mipmap, compressed_format)
  return t
end  

----------------------------------------------------------------------------
function gxl.set_sampler_params(tex, filtering, addressing, anisotropy)
  local SAMPLER_FILTERING_NEAREST = 1
  local SAMPLER_FILTERING_LINEAR = 2
  local SAMPLER_FILTERING_TRILINEAR = 3
  local SAMPLER_ADDRESSING_WRAP = 1
  local SAMPLER_ADDRESSING_CLAMP_TO_EDGE = 2
  local SAMPLER_ADDRESSING_MIRROR = 3
  local SAMPLER_ADDRESSING_CLAMP_TO_BORDER = 4
  
  local filter = SAMPLER_FILTERING_LINEAR
  if (filtering == "none") then
    filter = SAMPLER_FILTERING_NEAREST
  elseif (filtering == "linear") then
    filter = SAMPLER_FILTERING_LINEAR
  elseif (filtering == "trilinear") then
    filter = SAMPLER_FILTERING_TRILINEAR
  end

  local addr = SAMPLER_ADDRESSING_WRAP
  if (addressing == "wrap") then
    addr = SAMPLER_ADDRESSING_WRAP
  elseif (addressing == "clamp2edge") then
    addr = SAMPLER_ADDRESSING_CLAMP_TO_EDGE
  elseif (addressing == "clamp2border") then
    addr = SAMPLER_ADDRESSING_CLAMP_TO_BORDER
  elseif (addressing == "mirror") then
    addr = SAMPLER_ADDRESSING_MIRROR
  end
  
  gh_texture.bind(tex, 0)
  gh_texture.set_sampler_params(tex, filter, addr, anisotropy)
  gh_texture.bind(0, 0)
end
   

   
   
   
----------------------------------------------------------------------------
function gxl.random_init(seed)
  if (seed == 0) then
    seed = os.time()
  end
	math.randomseed(seed)
end

function gxl.random(a, b)
	if (a > b) then
		local c = b
		b = a
		a = c
	end
	local delta = b-a
	return (a + math.random()*delta)
end


--------------------------------------------------------------------
function gxl.to_radians(angle_degrees)
  return angle_degrees * 3.14159265 / 180.0
end


--------------------------------------------------------------------
function gxl.float2int(a)
  return math.floor(a + 0.5)
end  



--------------------------------------------------------------------
function gxl.is_func_available(lib_name, func_name)
  if (_G[lib_name][func_name] ~= nil) then
    return true
  end
  return false
end  



----------------------------------------------------------------------------
function gxl.create_depth_render_target(width, height)
  local linear_filtering = 1
  local rt = gh_render_target.create_depth(width, height, linear_filtering)
  return rt
end  

----------------------------------------------------------------------------
function gxl.create_color_render_target_rgba_u8(width, height, msaa)
  local rt = 0
  local PF_U8_RGBA = 3
  local num_targets = 1
  if (msaa > 0) then
    rt = gh_render_target.create_rb(width, height, PF_U8_RGBA, msaa)
  else
    rt = gh_render_target.create_ex(width, height, PF_U8_RGBA, num_targets, 0)
  end
  return rt
end  

----------------------------------------------------------------------------
function gxl.create_color_render_target_rgba_f32(width, height, msaa)
  local rt = 0
  local PF_F32_RGBA = 6
  local num_targets = 1
  if (msaa > 0) then
    rt = gh_render_target.create_rb(width, height, PF_F32_RGBA, msaa)
  else
    rt = gh_render_target.create_ex(width, height, PF_F32_RGBA, num_targets, 0)
  end
  return rt
end  

----------------------------------------------------------------------------
function gxl.create_color_render_target_rgba_f16(width, height, msaa)
  local rt = 0
  local PF_F16_RGBA = 9
  local num_targets = 1
  if (msaa > 0) then
    rt = gh_render_target.create_rb(width, height, PF_F16_RGBA, msaa)
  else
    rt = gh_render_target.create_ex(width, height, PF_F16_RGBA, num_targets, 0)
  end
  return rt
end  

----------------------------------------------------------------------------
function gxl.enable_msaa(state)
  gh_renderer.rasterizer_set_msaa_state(state)
	gh_renderer.rasterizer_apply_states()
end


----------------------------------------------------------------------------
function gxl.create_camera_3d(fov, width, height, znear, zfar)
  local aspect = 1.333
  if (height > 0) then
    aspect = width / height
  end  
  local camera = gh_camera.create_persp(fov, aspect, znear, zfar)
  gh_camera.set_viewport(camera, 0, 0, width, height)
  gh_camera.setpos(camera, 0, 10, 50)
  gh_camera.setlookat(camera, 0, 0, 0, 1)
  gh_camera.setupvec(camera, 0, 1, 0, 0)
  return camera
end  

----------------------------------------------------------------------------
function gxl.resize_camera_3d(camera, fov, width, height, znear, zfar)
  local aspect = 1.333
  if (height > 0) then
    aspect = width / height
  end  
  gh_camera.update_persp(camera, fov, aspect, znear, zfar)
  gh_camera.set_viewport(camera, 0, 0, width, height)
end

----------------------------------------------------------------------------
function gxl.create_camera_2d(width, height, znear, zfar)
  local camera = gh_camera.create_ortho(-width/2, width/2, -height/2, height/2, znear, zfar)
  gh_camera.set_viewport(camera, 0, 0, width, height)
  gh_camera.set_position(camera, 0, 0, 4)
  return camera
end  

----------------------------------------------------------------------------
function gxl.resize_camera_2d(camera, width, height, znear, zfar)
  gh_camera.update_ortho(camera, -width/2, width/2, -height/2, height/2, znear, zfar)
  gh_camera.set_viewport(camera, 0, 0, width, height)
end  


----------------------------------------------------------------------------
function gxl.cull_back_faces()
  local POLYGON_FACE_NONE = 0
  local POLYGON_FACE_BACK = 1
  local POLYGON_FACE_FRONT = 2
  local POLYGON_FACE_BACK_FRONT = 3
  gh_renderer.rasterizer_set_cull_state(1)
  gh_renderer.rasterizer_set_cull_face(POLYGON_FACE_BACK)
  gh_renderer.rasterizer_apply_states()
end  

function gxl.cull_front_faces()
  local POLYGON_FACE_NONE = 0
  local POLYGON_FACE_BACK = 1
  local POLYGON_FACE_FRONT = 2
  local POLYGON_FACE_BACK_FRONT = 3
  gh_renderer.rasterizer_set_cull_state(1)
  gh_renderer.rasterizer_set_cull_face(POLYGON_FACE_FRONT)
  gh_renderer.rasterizer_apply_states()
end  

function gxl.cull_none_faces()
  local POLYGON_FACE_NONE = 0
  local POLYGON_FACE_BACK = 1
  local POLYGON_FACE_FRONT = 2
  local POLYGON_FACE_BACK_FRONT = 3
  gh_renderer.rasterizer_set_cull_state(0)
  gh_renderer.rasterizer_set_cull_face(POLYGON_FACE_NONE)
  gh_renderer.rasterizer_apply_states()
end  


----------------------------------------------------------------------------
function gxl.print_quaternion(q)
  print(string.format("<%f ; %f ; %f ; %f>", q.x, q.y, q.z, q.w))
end

function gxl.imgui_print_quaternion(q)
  gh_imgui.text(string.format("<%f ; %f ; %f ; %f>", q.x, q.y, q.z, q.w))
end




----------------------------------------------------------------------------
-- HLS to RGB and RGB to HSL
-- via: https://stackoverflow.com/questions/2353211/hsl-to-rgb-color-conversion
-- https://en.wikipedia.org/wiki/HSL_and_HSV

function gxl._hue2rgb(p, q, t)
  if(t < 0) then t = t + 1 end
  if(t > 1) then t = t - 1 end
  if(t < 1/6) then return p + (q - p) * 6.0 * t end
  if(t < 1/2) then return q end
  if(t < 2/3) then return p + (q - p) * (2.0/3.0 - t) * 6.0 end
  return p
end

function gxl.hsl_to_rgb(h, s, l)
  local r, g, b

  if (s == 0.0) then
    r = l  -- achromatic
    g = l  -- achromatic
    b = l  -- achromatic
  else
    local q = 0.0

    if (l < 0.5 ) then
      q = l * (1.0 + s) 
    else
      q = l + s - l * s
    end

    local p = 2.0 * l - q
    r = gxl._hue2rgb(p, q, h + 1.0/3.0)
    g = gxl._hue2rgb(p, q, h);
    b = gxl._hue2rgb(p, q, h - 1.0/3.0)
  end

  return r, g, b
end

function gxl.rgb_to_hsl(r, g, b)
  local max = math.max(r, g, b)
  local min = math.min(r, g, b)
  local h, s, l = (max + min) / 2.0

  if(max == min) then
    h = 0
    s = 0 -- achromatic
  else
    local d = max - min
    local s = 0
    if (l > 0.5) then
      s = d / (2 - max - min)
    else
      s = d / (max + min)
    end
    
    if (max == r) then
      local x = 0.0
      if (g < b) then
        x = 6.0
      end
      h = (g - b) / d + x

    elseif (max == g) then
      h = (b - r) / d + 2
    
    elseif (max == b) then
      h = (r - g) / d + 4

    end

    h = h / 6.0
  end
  return h, s, l
end
--------------------------------------------------
