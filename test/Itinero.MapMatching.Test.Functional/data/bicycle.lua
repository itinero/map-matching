--[[
	Lua configuration file for the profiles used when routing, defines properties,
	preprocessing actions, routing behaviour and instruction generation.
--]]

name = "bicycle"
vehicle_types = { "vehicle", "bicycle" }
normalize = false

-- bicycle speeds
minspeed = 13
maxspeed = 13

speed_profile = {
	["primary"] = { speed = 15, access = true },
	["primary_link"] = { speed = 15, access = true },
	["secondary"] = { speed = 15, access = true },
	["secondary_link"] = { speed = 15, access = true },
	["tertiary"] = { speed = 15, access = true },
	["tertiary_link"] = { speed = 15, access = true },
	["unclassified"] = { speed = 15, access = true },
	["residential"] = { speed = 15, access = true },
	["service"] = { speed = 15, access = true },
	["services"] = { speed = 15, access = true },
	["road"] = { speed = 15, access = true },
	["track"] = { speed = 15, access = true },
	["cycleway"] = { speed = 15, access = true },
	["footway"] = { speed = 15, access = true },
	["pedestrian"] = { speed = 15, access = true },
	["path"] = { speed = 15, access = true },
	["living_street"] = { speed = 15, access = true },
	["ferry"] = { speed = 15, access = true },
	["movable"] = { speed = 15, access = true },
	["shuttle_train"] = { speed = 15, access = true },
	["default"] = { speed = 15, access = true }
}

-- default access values
access_factor_no = 0 -- totally no access.
access_factor_local = 0.01 -- only access when absolutely necessary.
access_factor_avoid = 0.1 -- can access but try to avoid.
access_factor_yes = 1 -- normal access.

access_values = {
    ["designated"] = access_factor_yes,
    ["public"] = access_factor_yes,
    ["yes"] = access_factor_yes,
    ["permissive"] = access_factor_avoid,
    ["destination"] = access_factor_yes,
    ["delivery"] = access_factor_avoid,
    ["service"] = access_factor_avoid,
    ["customers"] = access_factor_yes,
    ["private"] = access_factor_local,
    ["no"] = access_factor_no,
    ["use_sidepath"] = access_factor_no,
    ["gate"] = access_factor_no,
    ["bollard"] = access_factor_no
}

-- Properties to add to the metadata in the routerdb
profile_whitelist = {
	"highway",
	"oneway",
	"bicycle",
	"vehicle",
	"access",
	"maxspeed",
	"maxweight",
	"maxwidth",
	"roundabout",
	"junction",
	"cycleway",
	"cycleway:left",
	"cycleway:right",
	"cyclenetwork",
	"brussels",
	"genk",
	"oneway:bicycle",
	"operator",
	"cycleref",
	"cyclecolour",
	"surface",
	"railway",
	"parking:lane:both",
	"parking:lane:right",
	"anyways:construction",
	"anyways:access",
	"anyways:vehicle", 
	"anyways:bicycle"
}

-- Tags of the osm data to add to the metadata in the routerdb
meta_whitelist = {
	"name",
	"bridge",
	"tunnel",
	"colour",
	"ref",
	"status",
	"network"
}

-- The different profiles
profiles = {
	{ -- do not use, either incorrect time estimates or incorrect access restrictions.
		name = "",
		function_name = "factor_and_speed",
		metric = "time"
	},
	{ -- do not use, either incorrect time estimates or incorrect access restrictions.
		name = "shortest",
		function_name = "factor_and_speed",
		metric = "distance"
	},
    { -- this is the default profile, use this one for default routing.
        name = "default",
        function_name = "factor_and_speed",
        metric = "custom"
    },
    { -- this is the OPA profile, use this one for OPA routing.
        name = "opa",
        function_name = "factor_and_speed_opa",
        metric = "custom"
    },
	{
		name = "balanced",
		function_name = "factor_and_speed_balanced",
		metric = "custom"
	},
	{
		name = "networks",
		function_name = "factor_and_speed_networks",
		metric = "custom"
	},
	{
		name = "brussels",
		function_name = "factor_and_speed_networks_brussels",
		metric = "custom"
	},
	{
		name = "genk",
		function_name = "factor_and_speed_networks_genk",
		metric = "custom"
	},
	{
		name = "relaxed",
		function_name = "factor_and_speed_relaxed",
		metric = "custom"
	}
}

-- Processes the relation. All tags which are added to result.attributes_to_keep will be copied to 'attributes' of each individual way
function relation_tag_processor (attributes, result)
	result.attributes_to_keep = {}
	if attributes.network == "lcn" then
		result.attributes_to_keep.lcn = "yes"
	end
	if attributes.network == "rcn" then
		result.attributes_to_keep.rcn = "yes"
	end

	if (attributes.network == "lcn" or attributes.network == "rcn") and
			attributes.operator == "Stad Genk" then
		result.attributes_to_keep.genk = "yes"
		result.attributes_to_keep.operator = "Stad Genk"
	end
	
	if (attributes.network == "lcn" or attributes.network == "rcn") and
			attributes.operator == "Brussels Mobility" then
		result.attributes_to_keep.brussels = "yes"
		result.attributes_to_keep.operator = "Brussels Mobility"
	
	end
	
	if attributes.colour ~= nil and 
	    (result.attributes_to_keep.brussels == "yes" or result.attributes_to_keep.genk == "yes")
	        then
		result.attributes_to_keep.cyclecolour = attributes.colour
	end
	if attributes.ref ~= nil and (result.attributes_to_keep.brussels == "yes" or result.attributes_to_keep.genk == "yes") then
		result.attributes_to_keep.cycleref = attributes.ref
	end
	if attributes.type == "route" and
			attributes.route == "bicycle" then
		result.attributes_to_keep.cyclenetwork = "yes"
	end
end

-- processes node and adds the attributes to keep to the vertex meta collection in the routerdb.
function node_tag_processor (attributes, results)
	if attributes.rcn_ref then
		results.attributes_to_keep = {
			rcn_ref = attributes.rcn_ref
		}
	end
end

-- interprets access tags
function can_access (attributes, result)
    local last_access = {
        factor = nil,
        anyways = false 
    }
    
    -- first do access=x.
    local access = access_values[attributes.access]
    if access != nil then
        result.attributes_to_keep.access = true
        last_access.factor = access
    end
        
    -- then do motor_vehicle=x, etc.. based on the vehicle types above.    
    for i = 0, 10 do
        local access_key_key = vehicle_types[i]
        local access_key = attributes[access_key_key]
        if access_key then
            access = access_values[access_key]
            if access != nil then
                result.attributes_to_keep[access_key_key] = true
                last_access.factor = access
            end
        end
    end
    
    -- first do anyways:access=x.
    local access = access_values[attributes["anyways:access"]]
    if access != nil then
        result.attributes_to_keep["anyways:access"] = true
        last_access.factor = access
        last_access.anyways = true
    end

    -- then do the anyways overrides anyways:motor_vehicle=x, anyways:hgv=x etc.. based on the vehicle types above.    
    for i = 0, 10 do
        local access_key_key = vehicle_types[i]
        if access_key_key != nil then
            access_key_key = "anyways:" .. access_key_key
            local access_key = attributes[access_key_key]
            if access_key then
                access = access_values[access_key]
                if access != nil then
                    result.attributes_to_keep[access_key_key] = true
                    last_access.factor = access
                    last_access.anyways = true
                end
            end
        end
    end
    return last_access
end

-- Checks if this road is oneway for cyclists. Returns 'nil' if both ways accessible, 1 if oneway following the direction and 2 if oneway in the reversed direction
function is_oneway (attributes)

    if attributes["cycleway:opposite"] ~= nil and
        attributes["cycleway:opposite"] ~= "no"
        then
        -- There is a cycletrack to the other direction - we can go both ways
        return 0
    end

    local oneway = attributes["oneway"];
    if attributes["oneway:bicycle"] ~= nil then
        -- there is a oneway tag specifically for bicycles found
        -- We use that one instead 
        oneway = attributes["oneway:bicycle"]
    end
    
    if oneway == nil then
        -- No explicit oneway tag
        -- We can go both ways
        return 0
    end
    
	if oneway == "yes" or
		oneway == "true" or
		oneway == "1" then
		return 1
	end
	if oneway == "-1" then
		return 2
	end
	if oneway == "no" then
		return 0
	end
end

function factor_and_speed (attributes, result)
	local highway = attributes.highway

	result.speed = 0
	result.direction = 0
	result.canstop = true
	result.attributes_to_keep = {}
	
	if attributes.operator ~= nil
	then
	    result.attributes_to_keep.operator = attributes.operator
	end

	-- set highway to ferry when ferry.
	local route = attributes.route;
	if route == "ferry" then
		highway = "ferry"
		result.attributes_to_keep.route = highway
	end

	local highway_speed = speed_profile[highway]
	if highway_speed then
		result.speed = highway_speed.speed
		result.access = highway_speed.access
		result.direction = 0
		result.canstop = true
		result.attributes_to_keep.highway = highway
	else
		return
	end
    
    -- speed has been determined, now determine factor.
    -- a lower factor leads to lower weight for an edge.
    if result.speed == 0 then
        return
    end
    result.factor = 1.0 / (result.speed / 3.6)

    -- interpret access tags
    local access_factor = can_access(attributes, result)
    if access_factor.factor == 0 then
        -- only completely avoid when access factor is zero.
        result.speed = 0
        result.direction = 0
        result.canstop = true
        return
    end
    if access_factor.factor == nil then
        access_factor.factor = 1
    end
    if not access_factor.anyways then
        -- access was not determined by anyways access tags.
        
        -- remove access to construction roads
        if attributes["anyways:construction"] then
            result.speed = 0
            result.direction = 0
            result.canstop = false
            result.attributes_to_keep["anyways:construction"] = true
            return
        end
    end
    result.factor = result.factor / access_factor.factor

	-- get directional information
	local junction = attributes.junction
	if junction == "roundabout" then
		result.direction = 1
		result.attributes_to_keep.junction = true
	end
	local direction = is_oneway (attributes, "oneway")
	if direction ~= 0 then
		result.direction = direction
		result.attributes_to_keep.oneway = true
	end
end

highest_avoid_factor = 0.5
avoid_factor = 0.7
prefer_factor = 2
highest_prefer_factor = 3

-- multiplication factors per classification (balanced)
bicycle_balanced_factors = {
	["primary"] = highest_avoid_factor,
	["primary_link"] = highest_avoid_factor,
	["secondary"] = avoid_factor,
	["secondary_link"] = avoid_factor,
	["tertiary"] = 1,
	["tertiary_link"] = 1,
	["residential"] = 1,
	["path"] = prefer_factor,
	["cycleway"] = prefer_factor,
	["footway"] = prefer_factor,
	["pedestrian"] = avoid_factor,
	["steps"] = avoid_factor
}

bicycle_balanced_factors_cycleway = {
	["lane"] = avoid_factor,
	["track"] = prefer_factor,
	["shared_lane"] = prefer_factor,
	["opposite_lane"] = prefer_factor,
	["share_busway"] = prefer_factor,
	["opposite_track"] = prefer_factor,
	["opposite"] = prefer_factor
}

-- the factor function for the factor profile
function factor_and_speed_opa (attributes, result)

	factor_and_speed (attributes, result)

	if result.speed == 0 then
		return
	end

	-- result.factor = 1.0 / (result.speed / 3.6)
	local balanced_factor = bicycle_balanced_factors[attributes.highway]
	if balanced_factor ~= nil then
		result.factor = result.factor / balanced_factor
	else
		balanced_factor = 1
	end
	-- considers the cycleway key and its tag weights
	local cycleway_factor = bicycle_balanced_factors_cycleway[attributes["cycleway"]]
	if cycleway_factor ~= nil then
		balanced_factor = balanced_factor * cycleway_factor
	end
	-- considers the cycleway:left and cycleway:right keys together and the cycleway tag weights
	local cycleway_left_factor = bicycle_balanced_factors_cycleway[attributes["cycleway:left"]]
	local cycleway_right_factor = bicycle_balanced_factors_cycleway[attributes["cycleway:right"]]
	if cycleway_left_factor ~= nil and cycleway_right_factor ~= nil then
		balanced_factor = balanced_factor * (cycleway_right_factor - 0.1) * (cycleway_left_factor - 0.1)
	end

end

-- the factor function for the factor profile
function factor_and_speed_balanced (attributes, result)

	factor_and_speed (attributes, result)

	if result.speed == 0 then
		return
	end

	result.factor = 1.0 / (result.speed / 3.6)
	local balanced_factor = bicycle_balanced_factors[attributes.highway]
	if balanced_factor ~= nil then
		result.factor = result.factor / balanced_factor
	else
		balanced_factor = 1
	end
	-- considers the cycleway key and its tag weights
	local cycleway_factor = bicycle_balanced_factors_cycleway[attributes["cycleway"]]
	if cycleway_factor ~= nil then
		balanced_factor = balanced_factor * cycleway_factor
	end
	-- considers the cycleway:left and cycleway:right keys together and the cycleway tag weights
	local cycleway_left_factor = bicycle_balanced_factors_cycleway[attributes["cycleway:left"]]
	local cycleway_right_factor = bicycle_balanced_factors_cycleway[attributes["cycleway:right"]]
	if cycleway_left_factor ~= nil and cycleway_right_factor ~= nil then
		balanced_factor = balanced_factor * (cycleway_right_factor - 0.1) * (cycleway_left_factor - 0.1)
	end

end

-- multiplication factors per classification (relaxed)
bicycle_relaxed_factors_highway = {
	["primary"] = highest_avoid_factor,
	["primary_link"] = highest_avoid_factor,
	["secondary"] = highest_avoid_factor,
	["secondary_link"] = highest_avoid_factor,
	["tertiary"] = avoid_factor,
	["tertiary_link"] = avoid_factor,
	["residential"] = 1,
	["path"] = highest_prefer_factor,
	["cycleway"] = highest_prefer_factor,
	["footway"] = highest_prefer_factor,
	["pedestrian"] = 1,
	["steps"] = 1,
	["track"] = 1,
	["living_street"] = 1
}

bicycle_relaxed_factors_cycleway = {
	["lane"] = prefer_factor,
	["track"] = highest_prefer_factor,
	["shared_lane"] = prefer_factor,
	["opposite_lane"] = prefer_factor,
	["share_busway"] = highest_prefer_factor,
	["opposite_track"] = highest_prefer_factor,
	["opposite"] = prefer_factor
}

bicycle_relaxed_factors_surface = {
	["paving_stones"] = avoid_factor,
	["sett"] = avoid_factor,
	["cobblestone"] = avoid_factor,
	["gravel"] = avoid_factor,
	["pebblestone"] = avoid_factor,
	["unpaved"] = avoid_factor,
	["ground"] = avoid_factor,
	["dirt"] = highest_avoid_factor,
	["grass"] = highest_avoid_factor,
	["sand"] = highest_avoid_factor,
	["metal"] = 1,
	["paved"] = 1,
	["asphalt"] = 1,
	["concrete"] = 1,
	["fine_gravel"] = 1,
	["compacted"] = 1
}

bicycle_relaxed_factors_parking = {
	["parallel"] = avoid_factor
}

--[[
bicycle_relaxed_factors_bicycle = {
    ["yes"] = prefer_factor,
    ["no"] = 0
}
--]]
-- the factor function for the factor profile (relaxed)
function factor_and_speed_relaxed (attributes, result)

	factor_and_speed (attributes, result)

	if result.speed == 0 then
		return
	end
	-- considers the highway key and its tag weights
	result.factor = 1.0 / (result.speed / 3.6)
	local relaxed_factor = bicycle_relaxed_factors_highway[attributes.highway]
	if relaxed_factor == nil then
		relaxed_factor = 1;
	end
	-- considers the surface key and its tag weights
	local surface_factor = bicycle_relaxed_factors_surface[attributes.surface]
	if surface_factor ~= nil then
		relaxed_factor = relaxed_factor * surface_factor
	end
	-- considers the parking:lane:both key and its tag weights
	local parking_factor = bicycle_relaxed_factors_parking[attributes["parking:lane:both"]]
	if parking_factor ~= nil then
		relaxed_factor = relaxed_factor * parking_factor
	end
	-- considers the cycleway key and its tag weights
	local cycleway_factor = bicycle_relaxed_factors_cycleway[attributes["cycleway"]]
	if cycleway_factor ~= nil then
		relaxed_factor = relaxed_factor * cycleway_factor
	end
	-- considers the cycleway:left and cycleway:right keys together and the cycleway tag weights
	local cycleway_left_factor = bicycle_relaxed_factors_cycleway[attributes["cycleway:left"]]
	local cycleway_right_factor = bicycle_relaxed_factors_cycleway[attributes["cycleway:right"]]
	if cycleway_left_factor ~= nil and cycleway_right_factor ~= nil then
		relaxed_factor = relaxed_factor * (cycleway_right_factor - 0.1) * (cycleway_left_factor - 0.1)
	end

	result.factor = result.factor / relaxed_factor

end


function factor_and_speed_networks (attributes, result)

	factor_and_speed_balanced (attributes, result)

	if result.speed == 0 then
		return
	end

	if attributes.cyclenetwork == "yes" then
	    result.attributes_to_keep.cyclenetwork = yes
		result.factor = result.factor / 3
		result.attributes_to_keep["cyclenetwork"] = true
	end

end

--[[
	Function to calculate the factor of an edge in the graph when routing.
	If the edge is part of the brussels mobility network, favor it by a factor of 3.
--]]
function factor_and_speed_networks_brussels (attributes, result)
	factor_and_speed_balanced(attributes, result)
	if result.speed == 0 then
		return
	end

	if attributes.brussels then
		result.factor = result.factor / 3
		result.attributes_to_keep["brussels"] = true
	end
end

--[[
	Function to calculate the factor of an edge in the graph when routing.
	If the edge is part of the genk bicycle network, favor it by a factor of 3.
--]]
function factor_and_speed_networks_genk (attributes, result)
    
	factor_and_speed_balanced(attributes, result)
	if result.speed == 0 then
		return
	end

	if attributes.genk == "yes" then
	    -- itinero.log("Found a genk way, will divide the factor which now is"..tostring(result.factor))
		result.factor = result.factor / 3
		result.attributes_to_keep["genk"] = true
	end
end

-- instruction generators
instruction_generators = {
	{
		applies_to = "", -- applies to all profiles when empty
		generators = {
			{
				name = "start",
				function_name = "get_start"
			},
			{
				name = "stop",
				function_name = "get_stop"
			},
			{
				name = "roundabout",
				function_name = "get_roundabout"
			},
			{
				name = "turn",
				function_name = "get_turn"
			}
		}
	}
}

-- gets the first instruction
function get_start (route_position, language_reference, instruction)
	if route_position.is_first() then
		local direction = route_position.direction()
		instruction.text = itinero.format(language_reference.get("Start {0}."), language_reference.get(direction));
		instruction.shape = route_position.shape
		return 1
	end
	return 0
end

-- gets the last instruction
function get_stop (route_position, language_reference, instruction)
	if route_position.is_last() then
		instruction.text = language_reference.get("Arrived at destination.");
		instruction.shape = route_position.shape
		return 1
	end
	return 0
end

function contains (attributes, key, value)
	if attributes then
		return localvalue == attributes[key];
	end
end

-- gets a roundabout instruction
function get_roundabout (route_position, language_reference, instruction)
	if route_position.attributes.junction == "roundabout" and
			(not route_position.is_last()) then
		local attributes = route_position.next().attributes
		if attributes.junction then
		else
			local exit = 1
			local count = 1
			local previous = route_position.previous()
			while previous and previous.attributes.junction == "roundabout" do
				local branches = previous.branches
				if branches then
					branches = branches.get_traversable()
					if branches.count > 0 then
						exit = exit + 1
					end
				end
				count = count + 1
				previous = previous.previous()
			end

			instruction.text = itinero.format(language_reference.get("Take the {0}th exit at the next roundabout."), "" .. exit)
			if exit == 1 then
				instruction.text = itinero.format(language_reference.get("Take the first exit at the next roundabout."))
			elseif exit == 2 then
				instruction.text = itinero.format(language_reference.get("Take the second exit at the next roundabout."))
			elseif exit == 3 then
				instruction.text = itinero.format(language_reference.get("Take the third exit at the next roundabout."))
			end
			instruction.type = "roundabout"
			instruction.shape = route_position.shape
			return count
		end
	end
	return 0
end

-- gets a turn
function get_turn (route_position, language_reference, instruction)
	local relative_direction = route_position.relative_direction().direction

	local turn_relevant = false
	local branches = route_position.branches
	if branches then
		branches = branches.get_traversable()
		if relative_direction == "straighton" and
				branches.count >= 2 then
			turn_relevant = true -- straight on at cross road
		end
		if  relative_direction != "straighton" and
				branches.count > 0 then
			turn_relevant = true -- an actual normal turn
		end
	end

	if relative_direction == "unknown" then
		turn_relevant = false -- turn could not be calculated.
	end

	if turn_relevant then
		local next = route_position.next()
		local name = nil
		if next then
			name = next.attributes.name
		end
		if name then
			instruction.text = itinero.format(language_reference.get("Go {0} on {1}."),
				language_reference.get(relative_direction), name)
			instruction.shape = route_position.shape
		else
			instruction.text = itinero.format(language_reference.get("Go {0}."),
				language_reference.get(relative_direction))
			instruction.shape = route_position.shape
		end

		return 1
	end
	return 0
end

