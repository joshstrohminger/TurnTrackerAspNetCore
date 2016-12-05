/// <binding BeforeBuild='default, bump' Clean='clean' />

var fs = require('fs');
var gulp = require('gulp');
var rimraf = require('rimraf');
var merge = require('merge-stream');
var concat = require('gulp-concat');
var cssmin = require('gulp-cssmin');
var sass = require('gulp-sass');
var uglify = require('gulp-uglify');

var paths = {
    webroot: './wwwroot/',
    node: './node_modules/'
};

paths.lib = paths.webroot + 'lib/';
paths.libjs = paths.lib + 'js/';

paths.js = paths.webroot + 'js/**/*.js';
paths.minJs = paths.webroot + 'js/**/*.min.js';

paths.css = paths.webroot + 'css/**/*.css';
paths.minCss = paths.webroot + 'css/**/*.min.css';

paths.concatJsDest = paths.webroot + 'js/site.min.js';
paths.concatCssDest = paths.webroot + 'css/site.min.css';

gulp.task('clean:lib', function(cb) {
    return rimraf(paths.lib, cb);
});

gulp.task('clean:js', function (cb) {
    rimraf(paths.concatJsDest, cb);
});

gulp.task('clean:css', function (cb) {
    rimraf(paths.concatCssDest, cb);
});

gulp.task('clean', ['clean:js', 'clean:css', 'clean:lib']);

gulp.task('min:js', function () {
    return gulp.src([paths.js, '!' + paths.minJs], { base: '.' })
        .pipe(concat(paths.concatJsDest))
        .pipe(uglify())
        .pipe(gulp.dest('.'));
});

gulp.task('min:css', function () {
    return gulp.src([paths.css, '!' + paths.minCss])
        .pipe(sass())
        .pipe(concat(paths.concatCssDest))
        .pipe(cssmin())
        .pipe(gulp.dest('.'));
});

gulp.task('min', ['min:js', 'min:css']);

gulp.task('lib:js', function() {
    return gulp.src([
        paths.node + 'jquery/dist/**/*',
        paths.node + 'jquery-validation/dist/jquery.validate.js',
        paths.node + 'jquery-validation-unobtrusive/jquery.validate.unobtrusive.js'
    ]).pipe(gulp.dest(paths.libjs));
});

gulp.task('lib:bootstrap', function() {
    return gulp.src(paths.node + 'bootstrap/dist/**/*')
        .pipe(gulp.dest(paths.lib));
});

gulp.task('lib', ['lib:js', 'lib:bootstrap']);

gulp.task('default', ['lib', 'min']);

gulp.task('bump', function () {
    var filename = './project.json';
    var file = fs.readFileSync(filename, 'utf8');
    var found = file.match(/("version"\s*:\s*"\d+\.\d+\.\d+\.(\d+)")/);
    if (found == null || found.length !== 3) {
        console.error('failed to find version');
        return;
    }

    var version = found[1];
    var build = parseInt(found[2]);
    
    if (isNaN(build)) {
        console.error('failed to parse build number');
        return;
    }

    console.log('build version before bump: ' + build);
    build++;
    console.log('build version after bump: ' + build);

    var newVersion = version.substr(0, version.lastIndexOf('.') + 1) + build + '"';
    console.log('before: ' + version);
    console.log('after:  ' + newVersion);

    var edited = file.replace(version, newVersion);
    fs.writeFileSync(filename, edited);
});