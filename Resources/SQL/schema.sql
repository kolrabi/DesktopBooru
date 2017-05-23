-- -----------------------------------------------------------------------------

-- table containing per database application preference variables
DROP TABLE IF EXISTS config;
CREATE TABLE config 
(
    -- key
    "name"      VARCHAR(32) PRIMARY KEY     NOT NULL, 

    -- value
    "value"     TEXT                                    DEFAULT NULL
);

INSERT INTO config VALUES("$version", 0);

-- -----------------------------------------------------------------------------

-- table containing all imported images
DROP TABLE IF EXISTS images;
CREATE TABLE images 
(   
    -- binary md5 checksum uniquel identifying the image
    "md5sum"    BLOB(16)    PRIMARY KEY     NOT NULL,   

    -- type of image
    --   'I': standard still frame image
    --   'A': animated image (like gifs)
    --   'V': video files (containing audio, video)
    --   'C': "comic" file or other archive containing images
    "type"      CHAR( 1)                    NOT NULL    DEFAULT 'I',   

    -- elo ranking result from voting
    "elo"       FLOAT                       NOT NULL    DEFAULT 0.0,

    -- number of received votes
    "votes"     INTEGER                     NOT NULL    DEFAULT 0,
    "wins"      INTEGER                     NOT NULL    DEFAULT 0,
    "losses"    INTEGER                     NOT NULL    DEFAULT 0,

    -- image dimensions
    "width" 	INTEGER						NOT NULL 	DEFAULT 0,
    "height" 	INTEGER						NOT NULL 	DEFAULT 0,

    "added"     INTEGER 					NOT NULL 	DEFAULT ( STRFTIME('%s','now') ),
    "updated" 	INTEGER                                 DEFAULT NULL
);

-- index for finding an image by its checksum
CREATE INDEX idx_images_md5sum 
          ON images(md5sum ASC);

-- -----------------------------------------------------------------------------

-- table containing all imported files
DROP TABLE IF EXISTS files;
CREATE TABLE files 
(   
    -- path of file on filesystem
    "path"      TEXT        PRIMARY KEY     NOT NULL,   

    -- the md5sum of the file, identifying the image
    "md5sum"    BLOB(16)                    NOT NULL,   

    -- referencing an image, don't cascade on delete so we have its md5sum cached 
    FOREIGN KEY (md5sum) 
    REFERENCES  images(md5sum) 
);

-- index for finding an image by its path 
CREATE INDEX idx_files_path
          ON files(path);

-- index for finding a file for an image
CREATE INDEX idx_files_md5sum
          ON files(md5sum);

-- -----------------------------------------------------------------------------

DROP TABLE IF EXISTS tags;
CREATE TABLE tags 
(
    -- unique tag id, using tag string as id would also work but integers are faster
    "id"        INTEGER     PRIMARY KEY     NOT NULL, 

    -- name of tag
    "tag"       TEXT                        NOT NULL, 

    -- type of tag:
    --   0 = Normal:        Catch all for general tags
    --   1 = Series:        Tag names a series or collection of which an image can be part
    --   2 = Person:        Tag indicates an imaged person
    --   3 = Artist:        Tag indicates a person or group that created an image
    --   4 = Meta:          Tag may be used internally to organize images
    --   5 = Censorship:    Tag indicates the presence or absence or style of 
    --                      making parts of an image unrecognizable
    --   6 = Pose:          Tag indicates the pose of a person or character in an image
    --   7 = Location:      Tag indicates the location an image depicts
    --   8 = Clothing:      Tag indicates the presence or style of clothing in the image
    --   9 = Process:       Tag indicates an ongoing process in an image
    --  10 = Feature:       Tag indicates the presence of distinct image features
    "type"      INTEGER                     NOT NULL DEFAULT 0, 

    -- tag score based on image voting
    "score"     INTEGER                     NOT NULL DEFAULT 0,

    UNIQUE      (tag)
);

-- index for finding tag by id
CREATE INDEX idx_tags_id 
          ON tags(id ASC);

-- add some meta tags
INSERT INTO tags ("tag", "type") VALUES ("deleteme", 4);
INSERT INTO tags ("tag", "type") VALUES ("not_on_danbooru", 4);
INSERT INTO tags ("tag", "type") VALUES ("not_on_gelbooru", 4);
INSERT INTO tags ("tag", "type") VALUES ("known_on_danbooru", 4);
INSERT INTO tags ("tag", "type") VALUES ("known_on_gelbooru", 4);

-- -----------------------------------------------------------------------------

-- table containing the associations of tags to images
DROP TABLE IF EXISTS image_tags;
CREATE TABLE image_tags 
(   
    -- md5dsum of the image of associated tag
    "md5sum"    BLOB(16)                    NOT NULL,

    -- id of associated tag
    "tagid"     INTEGER                     NOT NULL,   

    -- delete tag associations for deleted images
    FOREIGN KEY (md5sum) 
    REFERENCES  images(md5sum)
    ON DELETE   CASCADE,   

    -- delete tag associations for deleted tags
    FOREIGN KEY (tagid)  
    REFERENCES  tags(id)
    ON DELETE   CASCADE,   

    -- an image can have a tag only once
    UNIQUE      (md5sum, tagid) 
);

-- index for finding tags for an image
CREATE INDEX idx_image_tags_md5sum 
          ON image_tags(md5sum ASC);

-- -----------------------------------------------------------------------------

-- table containing the implications a tag has
DROP TABLE IF EXISTS tag_implications;
CREATE TABLE tag_implications 
( 
    -- id of tag that implies another tag
    "tagid"     INTEGER                 NOT NULL, 

    -- id of implied tag
    "implies"   INTEGER                 NOT NULL, 

    -- if isneg is true, the implied tag is to be removed
    "isneg"     BOOLEAN                 NOT NULL    DEFAULT false, 

    -- delete implications for deleted tags
    FOREIGN KEY (tagid) 
    REFERENCES tags(id)
    ON DELETE   CASCADE, 

    -- delete implications for deleted implied tags
    FOREIGN KEY (implies) 
    REFERENCES tags(id)
    ON DELETE   CASCADE, 

    -- each tag can have each implied tag only once
    UNIQUE      (tagid, implies)
);

-- index for finding implications by id
CREATE INDEX idx_tag_implications_tagid 
          ON tag_implications(tagid ASC);
