# Maintainer: PatzminiHD  <youremail@domain.com>
pkgname=bbprog
pkgver=1.1.0
pkgrel=1
pkgdesc="BadBackupProgram. Backup a List of locations using rsync"
arch=('x86_64')
url="https://github.com/PatzminiHD/bbprog"
license=('GPL3')
depends=(
    "icu"
    "zlib"
    "rsync"
)
makedepends=(
    "git"
    "dotnet-host"
    "dotnet-sdk-6.0"
)
options=("staticlibs" "!strip")
# source=("${url}/releases/download/v${pkgver}/bsprog_${pkgver}_x86_64.tar.gz")
source=("git+${url}.git")
sha512sums=("SKIP")

build() {
  cd "bbprog"

  MSBUILDDISABLENODEREUSE=1 dotnet publish --self-contained --runtime linux-x64 --output ../$pkgname.tmp
}

package() {
  install -d $pkgdir/opt/
  install -d $pkgdir/usr/bin/

  cp -r $pkgname.tmp "$pkgdir/opt/$pkgname/"
  rm "$pkgdir/opt/$pkgname/bbprog.pdb"
  ln -s "/opt/$pkgname/$pkgname" "$pkgdir/usr/bin/$pkgname"
}
