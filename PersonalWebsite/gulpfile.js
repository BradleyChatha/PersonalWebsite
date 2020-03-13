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
let order       = require("gulp-order");
let webpack     = require("webpack-stream");

// Configure plugins
sass.compiler = require("sass");

// Path configs
const paths = {
    src: {
        sass: "Styles/site.scss",
        sass_watch: "Styles/*.scss",
        imgs_watch: "ImageRaw/**",
        imgs_index: "ImageRaw/Index/*.+(png|jpg)",
        webpack_index: "Scripts/bundles/index.js",
        vue_watch: "Scripts/**"
    },

    dest: {
        sass: "wwwroot/css",
        imgs_index_atlas: "wwwroot/img/atlas/index.png",
        imgs_index_css: "wwwroot/css/atlas_index.css",
        webpack_bundles: "wwwroot/js/"
    }
};

// Compile & Minify Our Sass
const cssOrder = [
    "**font-*",
    "**site*",
    "**highlight*",
    "**atlas_*"
];

gulp.task('compile-sass', function () {
    return gulp.src(paths.src.sass)
        .pipe(sass())
        .pipe(gulp.dest(paths.dest.sass));
});

gulp.task("minify-css", function () {
    return gulp.src(paths.dest.sass + "/!(bundle.min.css)")
        .pipe(order(cssOrder))
        .pipe(concat("bundle.min.css"))
        .pipe(cleanCSS({ level: 2 }))
        .pipe(gulp.dest(paths.dest.sass));
});

gulp.task("sass", gulp.series("compile-sass", "minify-css"));

// Create image atlases
const indexAtlas = {
    "+(cooper.*|selfie.*)": {
        width: 300,
        height: 300,
        rotate: 90
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
    return createAtlas(paths.src.imgs_index, paths.dest.imgs_index_atlas, paths.dest.imgs_index_css, indexAtlas);
});

gulp.task("atlas", gulp.series(["atlas-index"]));

// Compile vue templates with webpack
function compileBundle(src, bundleName) {
    const config = require("./webpack.config");
    config.output.filename = bundleName;

    return gulp.src(src)
            .pipe(webpack(config))
            .pipe(gulp.dest(paths.dest.webpack_bundles));
}

gulp.task("webpack-index", function(){
    return compileBundle(paths.src.webpack_index, "vue_index.js");
});

gulp.task("vue", gulp.series(["webpack-index"]));

// Watch Files For Changes
gulp.task('watch', function () {
    gulp.watch(paths.src.sass_watch, gulp.series(["sass"]));
    gulp.watch(paths.src.imgs_watch, gulp.series(["atlas"]));
    gulp.watch(paths.src.vue_watch, gulp.series(["vue"]));
});

// Default Task
gulp.task('default', gulp.series(["atlas", 'sass']));