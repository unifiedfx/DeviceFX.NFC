namespace DeviceFX.NfcApp.Model;

public record GeocodeResponse(
    string lat,
    string lon,
    int? place_id = null,
    string? licence = null,
    string? osm_type = null,
    int? osm_id = null,
    string? @class = null,
    string? type = null,
    int? place_rank = null,
    double? importance = null,
    string? addresstype = null,
    string? name = null,
    string? display_name = null,
    Address? address = null,
    string[]? boundingbox = null
);

public record Address(
    string road,
    string neighbourhood,
    string suburb,
    string town,
    string county,
    string ISO3166_2_lvl6,
    string state,
    string ISO3166_2_lvl4,
    string postcode,
    string country,
    string country_code
);