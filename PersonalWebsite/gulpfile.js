// Include gulp
let gulp = require('gulp');

// Include Our Plugins
let sass        = require('gulp-sass');
let cleanCSS    = require('gulp-clean-css');
let concat      = require("gulp-concat");
let spriteSmith = require("gulp.spritesmith");
let sharp       = require("sharp");
let minimatch   = require("minimatch");
let through     = require("through2");
let webp        = require("gulp-webp");
let buffer      = require("vinyl-buffer");
let flatten     = require("gulp-flatten");
let webpack     = require("webpack-stream");
let foreach     = require("gulp-foreach");
let addSrc      = require("gulp-add-src");
let path        = require("path");

// Configure plugins
sass.compiler = require("sass");

// Path configs
const paths = {
    src: {
        sass:                   "Styles/{site.scss,bundle-*.scss}",
        sass_watch:             "Styles/*.scss",
        imgs_watch:             "ImageRaw/**",
        imgs_index:             "ImageRaw/Index/*.+(png|jpg)",
        webpack_bundles:        "Scripts/bundles/",
        vue_watch:              "Scripts/**"
    },

    dest: {
        sass:               "Styles/build",
        sass_min:           "wwwroot/css",
        atlas_css:          "Styles/build/atlas",
        imgs_index_atlas:   "wwwroot/img/atlas/index.png",
        webpack_bundles:    "wwwroot/js/"
    }
};

// Compile & Minify Our Sass
gulp.task('compile-sass', function () {
    return gulp.src(paths.src.sass)
        .pipe(sass())
        .pipe(gulp.dest(paths.dest.sass));
});

gulp.task("minify-css", function () {
    return gulp.src(paths.dest.sass + "/bundle-*")
        .pipe(foreach((stream, file) => {
            return stream
                //.pipe(addSrc("Styles/extern/font-awesome.min.css"))
                .pipe(concat(path.basename(file.path)));
        }))
        .pipe(cleanCSS({ level: 2 }))
        .pipe(flatten())
        .pipe(gulp.dest(paths.dest.sass_min));
});

gulp.task("sass", gulp.series("compile-sass", "minify-css"));

// Create image atlases
const indexAtlas = {
    "selfie*": {
        width: 300,
        height: 300
    },
    "cooper*": {
        width: 300,
        height: 300,
        position: "top"
    },
    "nasm*": {
        width: 120,
        height: 120,
        fit: "inside"
    },
    "!{cooper,selfie}*": {
        width: 120,
        height: 120,
        fit: "fill"
    }
};

function cssGenerator(data) {
    let css = "";
    data.sprites.forEach(sprite => {
        css += `img.atlas.${sprite.name} { object-position: ${sprite.offset_x}px ${sprite.offset_y}px; width: ${sprite.width}px; height: ${sprite.height}px }`;
    });

    return css;
}

function createAtlas(sourcePath, imgName, cssName, atlasConfig) {
    return gulp.src(sourcePath)
        .pipe(buffer())
        .pipe(through.obj((file, enc, cb) => {
            for (const glob of Object.keys(atlasConfig)) {
                if (minimatch(file.basename, glob)) {
                    const config = atlasConfig[glob];

                    file.contents = sharp(file.contents)
                        .resize(null, null, config)
                        .rotate(config.rotate || 0);
                    break;
                }
            }

            return cb(null, file);
        }))
        .pipe(spriteSmith({
            imgName: imgName,
            cssName: cssName,
            cssTemplate: cssGenerator
        }))
        .pipe(buffer())
        .pipe(webp({ quality: 90 }))
        .pipe(gulp.dest("./"));
}

gulp.task("atlas-index", function () {
    return createAtlas(paths.src.imgs_index, paths.dest.imgs_index_atlas, paths.dest.atlas_css + "/index.css", indexAtlas);
});

gulp.task("atlas", gulp.series(["atlas-index"]));

// Compile vue templates with webpack
function compileBundle(bundleName) {
    const config = require("./webpack.config");
    config.output.filename = "vue_" + bundleName + ".js";

    return gulp.src(paths.src.webpack_bundles + bundleName + ".js")
            .pipe(webpack(config))
            .pipe(gulp.dest(paths.dest.webpack_bundles));
}

gulp.task("webpack-index", () => compileBundle("index"));
gulp.task("webpack-awards", () => compileBundle("awards"));

gulp.task("vue", gulp.series("webpack-index", "webpack-awards"));

// Watch Files For Changes
gulp.task('watch', function () {
    gulp.watch(paths.src.sass_watch, gulp.series(["sass"]));
    gulp.watch(paths.src.imgs_watch, gulp.series(["atlas"]));
    gulp.watch("Scripts/bundles/index.js", gulp.series("webpack-index"));
    gulp.watch("Scripts/components/language_card.vue", gulp.series("webpack-index"));
    gulp.watch("Scripts/bundles/awards.js", gulp.series("webpack-awards"));
});

// Default Task
gulp.task('default', gulp.series("atlas", 'sass'));