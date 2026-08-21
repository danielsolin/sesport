alter table public.entity_images
   add column thumbnail_data bytea,
   add column thumbnail_mime_type text,
   add column thumbnail_pixel_width integer,
   add column thumbnail_pixel_height integer,
   add column thumbnail_content_sha256 text,
   add column thumbnail_source_media_url text,
   add constraint entity_images_thumbnail_data_not_empty_check
      check (
         thumbnail_data is null
         or octet_length(thumbnail_data) > 0
      ),
   add constraint entity_images_thumbnail_mime_type_not_blank_check
      check (
         thumbnail_mime_type is null
         or btrim(thumbnail_mime_type) <> ''
      ),
   add constraint entity_images_thumbnail_mime_type_check
      check (
         thumbnail_data is null
         or thumbnail_mime_type is not null
      ),
   add constraint entity_images_thumbnail_pixel_width_check
      check (
         thumbnail_pixel_width is null
         or thumbnail_pixel_width > 0
      ),
   add constraint entity_images_thumbnail_pixel_height_check
      check (
         thumbnail_pixel_height is null
         or thumbnail_pixel_height > 0
      ),
   add constraint entity_images_thumbnail_sha256_check
      check (
         thumbnail_content_sha256 is null
         or thumbnail_content_sha256 ~ '^[0-9a-fA-F]{64}$'
      ),
   add constraint entity_images_thumbnail_source_url_check
      check (
         thumbnail_source_media_url is null
         or btrim(thumbnail_source_media_url) <> ''
      );
