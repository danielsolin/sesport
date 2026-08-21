create table public.entity_images
(
   id uuid primary key,
   entity_id uuid not null
      references public.entities(id) on delete cascade,

   image_data bytea,
   mime_type text,
   pixel_width integer,
   pixel_height integer,
   content_sha256 text,

   source_kind text not null,
   source_asset_id text,
   source_url text not null,
   source_media_url text,
   source_title text,
   creator_name text,
   creator_url text,
   license_name text not null,
   license_url text,
   copyright_notice text,
   attribution_text text,
   modification_description text,

   review_status text not null default 'candidate',
   review_note text,
   reviewed_at timestamp with time zone,
   is_primary boolean not null default false,

   retrieved_at timestamp with time zone default now() not null,
   created_at timestamp with time zone default now() not null,
   updated_at timestamp with time zone default now() not null,

   constraint entity_images_source_kind_not_blank_check
      check (btrim(source_kind) <> ''),
   constraint entity_images_source_asset_id_not_blank_check
      check (
         source_asset_id is null
         or btrim(source_asset_id) <> ''
      ),
   constraint entity_images_source_url_not_blank_check
      check (btrim(source_url) <> ''),
   constraint entity_images_source_media_url_not_blank_check
      check (
         source_media_url is null
         or btrim(source_media_url) <> ''
      ),
   constraint entity_images_image_data_not_empty_check
      check (
         image_data is null
         or octet_length(image_data) > 0
      ),
   constraint entity_images_mime_type_not_blank_check
      check (
         mime_type is null
         or btrim(mime_type) <> ''
      ),
   constraint entity_images_image_mime_type_check
      check (image_data is null or mime_type is not null),
   constraint entity_images_pixel_width_check
      check (pixel_width is null or pixel_width > 0),
   constraint entity_images_pixel_height_check
      check (pixel_height is null or pixel_height > 0),
   constraint entity_images_content_sha256_check
      check (
         content_sha256 is null
         or content_sha256 ~ '^[0-9a-fA-F]{64}$'
      ),
   constraint entity_images_license_name_not_blank_check
      check (btrim(license_name) <> ''),
   constraint entity_images_attribution_text_not_blank_check
      check (
         attribution_text is null
         or btrim(attribution_text) <> ''
      ),
   constraint entity_images_review_status_check
      check (
         review_status in (
            'candidate',
            'approved',
            'rejected',
            'withdrawn'
         )
      ),
   constraint entity_images_reviewed_status_check
      check (
         review_status = 'candidate'
         or reviewed_at is not null
      ),
   constraint entity_images_approved_data_check
      check (
         review_status <> 'approved'
         or image_data is not null
      ),
   constraint entity_images_primary_status_check
      check (not is_primary or review_status = 'approved')
);

create index entity_images_entity_status_idx
   on public.entity_images (entity_id, review_status, is_primary);

create index entity_images_source_asset_idx
   on public.entity_images (source_kind, source_asset_id)
   where source_asset_id is not null;

create unique index entity_images_entity_primary_unique
   on public.entity_images (entity_id)
   where is_primary;
