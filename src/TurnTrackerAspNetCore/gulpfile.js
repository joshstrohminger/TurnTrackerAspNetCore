/// <binding BeforeBuild='default' Clean='clean' />

var fs = require('fs');
var gulp = require('gulp');
var rimraf = require('rimraf');
var merge = require('merge-stream');
var concat = require('gulp-concat');
var cssmin = require('gulp-cssmin');
var sass = require('gulp-sass');
var uglify = require('gulp-uglify');
var filelog = require('gulp-filelog');

var paths = {
    webroot: './wwwroot/',
    node: './node_modules/',
    bower: './bower_components/'
};

paths.lib = paths.webroot + 'lib/';
paths.libjs = paths.lib + 'js/';
paths.libcss = paths.lib + 'css/';

paths.js = paths.webroot + 'js/**/*.js';
paths.minJs = paths.webroot + 'js/**/*.min.js';

paths.scss = paths.webroot + 'css/**/*.scss';

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
    return gulp.src(paths.scss)
        .pipe(filelog())
        .pipe(sass())
        .pipe(concat(paths.concatCssDest))
        .pipe(cssmin())
        .pipe(gulp.dest('.'));
});

gulp.task('min', ['min:js', 'min:css']);

gulp.task('lib:js', function() {
    return gulp.src([
        paths.bower + 'jquery/dist/**/*',
        paths.bower + 'moment/min/moment.min.js',
        paths.bower + 'eonasdan-bootstrap-datetimepicker/build/js/bootstrap-datetimepicker.min.js',
        paths.bower + 'jquery-validation/dist/jquery.validate.js',
        paths.bower + 'jquery-validation-unobtrusive/jquery.validate.unobtrusive.js'
    ]).pipe(gulp.dest(paths.libjs));
});

gulp.task('lib:bootstrap', function() {
    return gulp.src(paths.bower + 'bootstrap/dist/**/*')
        .pipe(gulp.dest(paths.lib));
});

gulp.task('lib:css', function() {
    return gulp.src([
        paths.bower + 'eonasdan-bootstrap-datetimepicker/build/css/bootstrap-datetimepicker.min.css'
    ]).pipe(gulp.dest(paths.libcss));
});

gulp.task('lib', ['lib:js', 'lib:bootstrap', 'lib:css']);

gulp.task('default', ['lib', 'min']);

gulp.task('bump', function () {
    var filename = './project.json';
    var file = fs.readFileSync(filename, 'utf8');
    var found = file.match(/("version"\s*:\s*"\d+\.\d+\.\d+\.(\d+)")/);
    if (found === null || found.length !== 3) {
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